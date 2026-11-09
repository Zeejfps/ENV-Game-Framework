using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using static GL46;
using static OpenGLSandbox.OpenGlUtils;

namespace ZGF.Gui.OpenGL.Windows;

/// <summary>
/// Copies D3D11 shared textures into textures GL can sample, through <c>WGL_NV_DX_interop2</c>: a D3D11
/// device on the GL context's adapter opens each shared handle and copies it into a texture this bridge
/// owns, which is registered with GL, and GL copies that, locked only for the copy, into an ordinary texture any
/// context of the share group can sample. Everything it does with GL and WGL runs with its home context current, one
/// that outlives it, whichever context the caller has current.
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
    private readonly WglContext _home;
    private uint _readFramebuffer;

    private D3D11GlBridge(void* device, void* context, nint interopDevice, WglDxInterop wgl, WglContext home)
    {
        _device = device;
        _context = context;
        _interopDevice = interopDevice;
        _wgl = wgl;
        _home = home;
    }

    /// <param name="home">The context every interop call runs under, which must outlive the bridge and every texture it makes.</param>
    public static D3D11GlBridge? TryCreate(GetProcAddressDelegate getProcAddress, WglContext home, out string failure)
    {
        using var scope = WglContext.Enter(home, "opening the D3D11 interop device");
        if (!scope.Entered)
        {
            failure = "its home context could not be made current";
            return null;
        }
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
        return new D3D11GlBridge(device, context, interopDevice, wgl, home);
    }

    /// <summary>Copies the texture behind <paramref name="ntHandle"/> into <paramref name="current"/>, or into a new
    /// texture returned as <paramref name="target"/> when it is null or its size or format no longer match, leaving
    /// <paramref name="current"/> for the caller to free. A failure leaves <paramref name="current"/> as it was.</summary>
    public bool TryCopy(nint ntHandle, D3D11GlTexture? current, [NotNullWhen(true)] out D3D11GlTexture? target, out string failure)
    {
        target = null;
        using var scope = WglContext.Enter(_home, "copying a shared texture");
        if (!scope.Entered)
        {
            failure = "the bridge's home context could not be made current";
            return false;
        }
        var hr = D3D11.OpenSharedTexture(_device, ntHandle, out var source);
        if (hr < 0)
        {
            failure = $"OpenSharedResource1 failed (0x{hr:X8})";
            return false;
        }

        try
        {
            var desc = D3D11.GetDesc(source);
            var into = current;
            if (into == null || !into.Matches(desc))
            {
                into = CreateTexture(desc, out failure);
                if (into == null)
                    return false;
            }

            D3D11.CopyResource(_context, into.D3DTexture, source);
            D3D11.Flush(_context);
            if (!into.CopyForSampling(ReadFramebuffer(), out failure))
            {
                if (into != current)
                    into.Dispose();
                return false;
            }
            target = into;
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

        uint interopTexture;
        glGenTextures(1, &interopTexture);
        var interopObject = _wgl.RegisterObject(_interopDevice, texture, interopTexture, GL_TEXTURE_2D, WglAccessReadOnly);
        if (interopObject == 0)
        {
            glDeleteTextures(1, &interopTexture);
            D3D11.Release(texture);
            failure = "wglDXRegisterObjectNV failed";
            return null;
        }

        uint glTexture;
        glGenTextures(1, &glTexture);
        glBindTexture(GL_TEXTURE_2D, glTexture);
        glTexImage2D(GL_TEXTURE_2D, 0, (int)GL_RGBA8, (int)desc.Width, (int)desc.Height, 0, GL_RGBA, GL_UNSIGNED_BYTE, (void*)0);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, (int)GL_LINEAR);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, (int)GL_LINEAR);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, (int)GL_CLAMP_TO_EDGE);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, (int)GL_CLAMP_TO_EDGE);
        glBindTexture(GL_TEXTURE_2D, 0);
        AssertNoGlError();

        failure = string.Empty;
        return new D3D11GlTexture(_wgl, _interopDevice, _home, texture, interopTexture, interopObject, glTexture, desc);
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

    /// <summary>The framebuffer GL reads a registered texture through, made in the home context, whose framebuffers
    /// are its own.</summary>
    private uint ReadFramebuffer()
    {
        if (_readFramebuffer != 0)
            return _readFramebuffer;
        uint framebuffer;
        glGenFramebuffers(1, &framebuffer);
        _readFramebuffer = framebuffer;
        return framebuffer;
    }

    public void Dispose()
    {
        using (var scope = WglContext.Enter(_home, "closing the D3D11 interop device"))
        {
            if (scope.Entered)
            {
                if (_readFramebuffer != 0)
                {
                    var framebuffer = _readFramebuffer;
                    glDeleteFramebuffers(1, &framebuffer);
                    _readFramebuffer = 0;
                }
                if (!_wgl.CloseDevice(_interopDevice))
                    Console.Error.WriteLine("[D3D11GlBridge] wglDXCloseDeviceNV failed");
            }
        }
        D3D11.Release(_context);
        D3D11.Release(_device);
    }
}

/// <summary>A D3D11 texture registered with GL, and the ordinary GL texture canvases sample in its place. The registered
/// texture is locked only while GL copies it into that one, with the bridge's home context current, so no other
/// context ever touches it; the copy is an ordinary texture of the share group, which any context may sample.</summary>
[SupportedOSPlatform("windows")]
internal sealed unsafe class D3D11GlTexture : IDisposable
{
    private readonly WglDxInterop _wgl;
    private readonly nint _interopDevice;
    private readonly WglContext _home;
    private readonly D3D11.Texture2DDesc _desc;
    private readonly uint _interopTexture;
    private nint _interopObject;

    public D3D11GlTexture(WglDxInterop wgl, nint interopDevice, WglContext home, void* d3dTexture, uint interopTexture,
        nint interopObject, uint glTexture, in D3D11.Texture2DDesc desc)
    {
        _wgl = wgl;
        _interopDevice = interopDevice;
        _home = home;
        D3DTexture = d3dTexture;
        _interopTexture = interopTexture;
        _interopObject = interopObject;
        GlTexture = glTexture;
        _desc = desc;
    }

    public void* D3DTexture { get; }

    /// <summary>The ordinary texture canvases sample.</summary>
    public uint GlTexture { get; }

    public int Width => (int)_desc.Width;
    public int Height => (int)_desc.Height;

    public bool Matches(in D3D11.Texture2DDesc desc) =>
        desc.Width == _desc.Width && desc.Height == _desc.Height && desc.Format == _desc.Format;

    /// <summary>Copies what D3D11 last wrote into <see cref="GlTexture"/>, locking the registered texture for just the
    /// copy, through <paramref name="readFramebuffer"/>, a framebuffer of the home context.</summary>
    public bool CopyForSampling(uint readFramebuffer, out string failure)
    {
        using var home = WglContext.Enter(_home, "copying a shared texture for sampling");
        if (!home.Entered)
        {
            failure = "the bridge's home context could not be made current";
            return false;
        }
        var handle = _interopObject;
        if (!_wgl.LockObjects(_interopDevice, 1, &handle))
        {
            failure = "wglDXLockObjectsNV failed";
            return false;
        }

        int previous;
        glGetIntegerv(GL_READ_FRAMEBUFFER_BINDING, &previous);
        glBindFramebuffer(GL_READ_FRAMEBUFFER, readFramebuffer);
        glFramebufferTexture2D(GL_READ_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_TEXTURE_2D, _interopTexture, 0);
        glReadBuffer(GL_COLOR_ATTACHMENT0);
        glBindTexture(GL_TEXTURE_2D, GlTexture);
        glCopyTexSubImage2D(GL_TEXTURE_2D, 0, 0, 0, 0, 0, Width, Height);
        glBindTexture(GL_TEXTURE_2D, 0);
        glFramebufferTexture2D(GL_READ_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_TEXTURE_2D, 0, 0);
        glBindFramebuffer(GL_READ_FRAMEBUFFER, (uint)previous);
        var error = glGetError();
        glFlush();

        var unlocked = _wgl.UnlockObjects(_interopDevice, 1, &handle);
        if (error != GL_NO_ERROR)
        {
            failure = $"copying the shared texture failed (GL error 0x{error:X4})";
            return false;
        }
        if (!unlocked)
        {
            failure = "wglDXUnlockObjectsNV failed";
            return false;
        }
        failure = string.Empty;
        return true;
    }

    public void Dispose()
    {
        using var home = WglContext.Enter(_home, "releasing a shared texture");
        if (!home.Entered)
        {
            Console.Error.WriteLine("[D3D11GlBridge] a shared texture is left registered: its home context could not be made current");
            return;
        }
        if (!_wgl.UnregisterObject(_interopDevice, _interopObject))
            Console.Error.WriteLine("[D3D11GlBridge] wglDXUnregisterObjectNV failed");
        _interopObject = 0;
        var interopTexture = _interopTexture;
        glDeleteTextures(1, &interopTexture);
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
    public bool CloseDevice(nint device) => _closeDevice(device) != 0;
    public nint RegisterObject(nint device, void* d3dObject, uint glName, uint glType, uint access) =>
        _registerObject(device, d3dObject, glName, glType, access);
    public bool UnregisterObject(nint device, nint interopObject) => _unregisterObject(device, interopObject) != 0;
    public bool LockObjects(nint device, int count, nint* objects) => _lockObjects(device, count, objects) != 0;
    public bool UnlockObjects(nint device, int count, nint* objects) => _unlockObjects(device, count, objects) != 0;
}
