using System.Runtime.Versioning;
using static GL46;
using static OpenGLSandbox.OpenGlUtils;

namespace ZGF.Gui.OpenGL.Windows;

/// <summary>
/// Copies D3D11 shared textures into textures GL can sample, through <c>WGL_NV_DX_interop2</c>: a D3D11
/// device on the GL context's adapter opens each shared handle and copies it into a texture this bridge
/// owns, which is registered with GL. Every GL call needs a context of the canvas share group current.
/// </summary>
[SupportedOSPlatform("windows")]
internal sealed unsafe class D3D11GlBridge : IDisposable
{
    private const uint WglAccessReadOnly = 0;
    private const uint GlDeviceLuid = 0x9599;

    private readonly void* _device;
    private readonly void* _context;
    private readonly nint _interopDevice;
    private readonly WglDxInterop _wgl;

    private D3D11GlBridge(void* device, void* context, nint interopDevice, WglDxInterop wgl)
    {
        _device = device;
        _context = context;
        _interopDevice = interopDevice;
        _wgl = wgl;
    }

    public static D3D11GlBridge? TryCreate(GetProcAddressDelegate getProcAddress, out string failure)
    {
        var wgl = WglDxInterop.TryLoad(getProcAddress);
        if (wgl == null)
        {
            failure = "WGL_NV_DX_interop is not available";
            return null;
        }

        var hr = D3D11.CreateDevice(ReadGlAdapterLuid(getProcAddress), out var device, out var context);
        if (hr < 0)
        {
            failure = $"D3D11CreateDevice failed (0x{hr:X8})";
            return null;
        }

        var interopDevice = wgl.OpenDevice(device);
        if (interopDevice == 0)
        {
            D3D11.Release(context);
            D3D11.Release(device);
            failure = "wglDXOpenDeviceNV failed";
            return null;
        }

        failure = string.Empty;
        return new D3D11GlBridge(device, context, interopDevice, wgl);
    }

    /// <summary>Copies the texture behind <paramref name="ntHandle"/> into <paramref name="target"/>,
    /// replacing <paramref name="target"/> with a new texture when it is null or its size or format no
    /// longer match.</summary>
    public bool TryCopy(nint ntHandle, ref D3D11GlTexture? target, out string failure)
    {
        var hr = D3D11.OpenSharedTexture(_device, ntHandle, out var source);
        if (hr < 0)
        {
            failure = $"OpenSharedResource1 failed (0x{hr:X8})";
            return false;
        }

        try
        {
            var desc = D3D11.GetDesc(source);
            if (target == null || !target.Matches(desc))
            {
                target?.Dispose();
                target = null;
                var created = CreateTexture(desc, out failure);
                if (created == null)
                    return false;
                target = created;
            }

            target.Unlock();
            D3D11.CopyResource(_context, target.D3DTexture, source);
            D3D11.Flush(_context);
            target.Lock();
            failure = string.Empty;
            return true;
        }
        finally
        {
            D3D11.Release(source);
        }
    }

    private D3D11GlTexture? CreateTexture(in D3D11.Texture2DDesc sourceDesc, out string failure)
    {
        var desc = new D3D11.Texture2DDesc
        {
            Width = sourceDesc.Width,
            Height = sourceDesc.Height,
            MipLevels = 1,
            ArraySize = 1,
            Format = sourceDesc.Format,
            SampleCount = 1,
            BindFlags = D3D11.BindShaderResource | D3D11.BindRenderTarget,
        };
        var hr = D3D11.CreateTexture2D(_device, desc, out var texture);
        if (hr < 0)
        {
            failure = $"CreateTexture2D {desc.Width}x{desc.Height} failed (0x{hr:X8})";
            return null;
        }

        uint glTexture;
        glGenTextures(1, &glTexture);
        var interopObject = _wgl.RegisterObject(_interopDevice, texture, glTexture, GL_TEXTURE_2D, WglAccessReadOnly);
        if (interopObject == 0)
        {
            glDeleteTextures(1, &glTexture);
            D3D11.Release(texture);
            failure = "wglDXRegisterObjectNV failed";
            return null;
        }

        glBindTexture(GL_TEXTURE_2D, glTexture);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, (int)GL_LINEAR);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, (int)GL_LINEAR);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, (int)GL_CLAMP_TO_EDGE);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, (int)GL_CLAMP_TO_EDGE);
        glBindTexture(GL_TEXTURE_2D, 0);
        AssertNoGlError();

        failure = string.Empty;
        return new D3D11GlTexture(_wgl, _interopDevice, texture, glTexture, interopObject, desc);
    }

    // GL_EXT_memory_object reports the adapter the context runs on; without it, the default adapter.
    private static ulong? ReadGlAdapterLuid(GetProcAddressDelegate getProcAddress)
    {
        var getUnsignedBytev = (delegate* unmanaged<uint, byte*, void>)getProcAddress("glGetUnsignedBytevEXT");
        if (getUnsignedBytev == null)
            return null;
        ulong luid = 0;
        getUnsignedBytev(GlDeviceLuid, (byte*)&luid);
        return glGetError() == GL_NO_ERROR && luid != 0 ? luid : null;
    }

    public void Dispose()
    {
        _wgl.CloseDevice(_interopDevice);
        D3D11.Release(_context);
        D3D11.Release(_device);
    }
}

/// <summary>A D3D11 texture registered with GL. It stays locked for GL except while D3D11 copies into
/// it, so GL may sample it at any time between copies.</summary>
[SupportedOSPlatform("windows")]
internal sealed unsafe class D3D11GlTexture : IDisposable
{
    private readonly WglDxInterop _wgl;
    private readonly nint _interopDevice;
    private readonly D3D11.Texture2DDesc _desc;
    private nint _interopObject;
    private bool _locked;

    public D3D11GlTexture(WglDxInterop wgl, nint interopDevice, void* d3dTexture, uint glTexture, nint interopObject, in D3D11.Texture2DDesc desc)
    {
        _wgl = wgl;
        _interopDevice = interopDevice;
        D3DTexture = d3dTexture;
        GlTexture = glTexture;
        _interopObject = interopObject;
        _desc = desc;
    }

    public void* D3DTexture { get; }
    public uint GlTexture { get; }
    public int Width => (int)_desc.Width;
    public int Height => (int)_desc.Height;

    public bool Matches(in D3D11.Texture2DDesc desc) =>
        desc.Width == _desc.Width && desc.Height == _desc.Height && desc.Format == _desc.Format;

    public void Lock()
    {
        if (_locked) return;
        var handle = _interopObject;
        _locked = _wgl.LockObjects(_interopDevice, 1, &handle);
    }

    public void Unlock()
    {
        if (!_locked) return;
        var handle = _interopObject;
        _wgl.UnlockObjects(_interopDevice, 1, &handle);
        _locked = false;
    }

    public void Dispose()
    {
        Unlock();
        _wgl.UnregisterObject(_interopDevice, _interopObject);
        _interopObject = 0;
        var glTexture = GlTexture;
        glDeleteTextures(1, &glTexture);
        D3D11.Release(D3DTexture);
    }
}

/// <summary>The <c>WGL_NV_DX_interop</c> entry points, resolved through the GL loader with a context
/// current.</summary>
[SupportedOSPlatform("windows")]
internal sealed unsafe class WglDxInterop
{
    private readonly delegate* unmanaged<void*, nint> _openDevice;
    private readonly delegate* unmanaged<nint, int> _closeDevice;
    private readonly delegate* unmanaged<nint, void*, uint, uint, uint, nint> _registerObject;
    private readonly delegate* unmanaged<nint, nint, int> _unregisterObject;
    private readonly delegate* unmanaged<nint, int, nint*, int> _lockObjects;
    private readonly delegate* unmanaged<nint, int, nint*, int> _unlockObjects;

    private WglDxInterop(nint open, nint close, nint register, nint unregister, nint lockObjects, nint unlockObjects)
    {
        _openDevice = (delegate* unmanaged<void*, nint>)open;
        _closeDevice = (delegate* unmanaged<nint, int>)close;
        _registerObject = (delegate* unmanaged<nint, void*, uint, uint, uint, nint>)register;
        _unregisterObject = (delegate* unmanaged<nint, nint, int>)unregister;
        _lockObjects = (delegate* unmanaged<nint, int, nint*, int>)lockObjects;
        _unlockObjects = (delegate* unmanaged<nint, int, nint*, int>)unlockObjects;
    }

    public static WglDxInterop? TryLoad(GetProcAddressDelegate getProcAddress)
    {
        var open = getProcAddress("wglDXOpenDeviceNV");
        var close = getProcAddress("wglDXCloseDeviceNV");
        var register = getProcAddress("wglDXRegisterObjectNV");
        var unregister = getProcAddress("wglDXUnregisterObjectNV");
        var lockObjects = getProcAddress("wglDXLockObjectsNV");
        var unlockObjects = getProcAddress("wglDXUnlockObjectsNV");
        if (open == 0 || close == 0 || register == 0 || unregister == 0 || lockObjects == 0 || unlockObjects == 0)
            return null;
        return new WglDxInterop(open, close, register, unregister, lockObjects, unlockObjects);
    }

    public nint OpenDevice(void* d3dDevice) => _openDevice(d3dDevice);
    public void CloseDevice(nint device) => _closeDevice(device);
    public nint RegisterObject(nint device, void* d3dObject, uint glName, uint glType, uint access) =>
        _registerObject(device, d3dObject, glName, glType, access);
    public void UnregisterObject(nint device, nint interopObject) => _unregisterObject(device, interopObject);
    public bool LockObjects(nint device, int count, nint* objects) => _lockObjects(device, count, objects) != 0;
    public void UnlockObjects(nint device, int count, nint* objects) => _unlockObjects(device, count, objects);
}
