using System.Numerics;
using System.Runtime.Versioning;
using PngSharp.Api;
using PngSharp.Spec.Chunks.IHDR;
using ZGF.Gui.OpenGL.Windows;
using static GL46;
using static OpenGLSandbox.OpenGlUtils;

namespace ZGF.Gui.OpenGL;

public sealed unsafe class GlImageManager : IDisposable
{
    private readonly Dictionary<string, uint> _textureIdByImageId = new();
    private readonly Dictionary<string, Size> _sizeByImageId = new();
    private readonly List<uint> _ownedTextures = new();
    private readonly List<uint> _ownedFrameBuffers = new();
    [SupportedOSPlatform("windows")]
    private readonly Dictionary<string, D3D11GlTexture> _sharedByImageId = new();
    private readonly GetProcAddressDelegate? _getProcAddress;
    private SharedTextureBridge _bridge = new SharedTextureBridge.NotCreated();
    private readonly HeldTextures<RetiredTexture> _held;
    [SupportedOSPlatform("windows")]
    private readonly WglContext _home;

    /// <summary>Make it with a context current that outlives it, such as the main window's, which owns the share
    /// group: shared textures are imported and released under that context.</summary>
    /// <param name="getProcAddress">Resolves GL and WGL entry points for the shared-texture bridge;
    /// without it the manager imports no shared textures.</param>
    public GlImageManager(GetProcAddressDelegate? getProcAddress = null)
    {
        _getProcAddress = getProcAddress;
        _held = new HeldTextures<RetiredTexture>(ReleaseRetired);
        if (OperatingSystem.IsWindows())
            _home = WglContext.Current;
    }

    /// <summary>How many textures of removed or replaced images a canvas frame still holds.</summary>
    public int RetiredCount => _held.RetiredCount;

    /// <summary>A canvas staged a draw of <paramref name="texture"/>, which must stay alive until it releases it.</summary>
    public void HoldTexture(uint texture) => _held.Hold(texture);

    /// <summary>The canvas will not draw <paramref name="texture"/> again.</summary>
    public void ReleaseTexture(uint texture) => _held.Release(texture);

    /// <summary>A texture no image names any more, kept while a canvas frame holds it.</summary>
    private abstract record RetiredTexture
    {
        public sealed record Owned(uint Texture) : RetiredTexture;

        [SupportedOSPlatform("windows")]
        public sealed record Shared(D3D11GlTexture Texture) : RetiredTexture;
    }

    private void ReleaseRetired(RetiredTexture retired)
    {
        switch (retired)
        {
            case RetiredTexture.Owned owned:
                var texture = owned.Texture;
                _ownedTextures.Remove(texture);
                glDeleteTextures(1, &texture);
                break;
            case RetiredTexture.Shared shared when OperatingSystem.IsWindows():
                shared.Texture.Dispose();
                break;
            default:
                throw new InvalidOperationException($"Unknown retired texture {retired}.");
        }
    }

    public void LoadImageFromFile(string pathToImageFile)
    {
        if (_textureIdByImageId.ContainsKey(pathToImageFile))
            return;

        var png = Png.DecodeFromFile(pathToImageFile);
        var width = (int)png.Ihdr.Width;
        var height = (int)png.Ihdr.Height;
        var rgba = DecodeToRgba(png);

        var textureId = UploadRgbaTexture(width, height, rgba);
        _textureIdByImageId.Add(pathToImageFile, textureId);
        _sizeByImageId.Add(pathToImageFile, new Size { Width = width, Height = height });
        _ownedTextures.Add(textureId);
    }

    public GlFrameBufferHandle CreateFrameBuffer(int width, int height)
    {
        uint frameBufferId;
        glGenFramebuffers(1, &frameBufferId);
        AssertNoGlError();

        glBindFramebuffer(GL_FRAMEBUFFER, frameBufferId);
        AssertNoGlError();

        uint colorTextureId;
        glGenTextures(1, &colorTextureId);
        glBindTexture(GL_TEXTURE_2D, colorTextureId);
        glTexImage2D(GL_TEXTURE_2D, 0, (int)GL_RGBA8, width, height, 0, GL_RGBA, GL_UNSIGNED_BYTE, (void*)0);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, (int)GL_LINEAR);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, (int)GL_LINEAR);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, (int)GL_CLAMP_TO_EDGE);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, (int)GL_CLAMP_TO_EDGE);
        glFramebufferTexture2D(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_TEXTURE_2D, colorTextureId, 0);
        AssertNoGlError();

        uint depthTextureId;
        glGenTextures(1, &depthTextureId);
        glBindTexture(GL_TEXTURE_2D, depthTextureId);
        glTexImage2D(GL_TEXTURE_2D, 0, (int)GL_DEPTH_COMPONENT24, width, height, 0, GL_DEPTH_COMPONENT, GL_FLOAT, (void*)0);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, (int)GL_NEAREST);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, (int)GL_NEAREST);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, (int)GL_CLAMP_TO_EDGE);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, (int)GL_CLAMP_TO_EDGE);
        glFramebufferTexture2D(GL_FRAMEBUFFER, GL_DEPTH_ATTACHMENT, GL_TEXTURE_2D, depthTextureId, 0);
        AssertNoGlError();

        var attachment = GL_COLOR_ATTACHMENT0;
        glDrawBuffers(1, &attachment);
        AssertNoGlError();

        if (glCheckFramebufferStatus(GL_FRAMEBUFFER) != GL_FRAMEBUFFER_COMPLETE)
            throw new Exception("Framebuffer not complete");

        glBindFramebuffer(GL_FRAMEBUFFER, 0);
        AssertNoGlError();

        var imageId = $"framebuffer_{frameBufferId}";
        _textureIdByImageId.Add(imageId, colorTextureId);
        _sizeByImageId.Add(imageId, new Size { Width = width, Height = height });
        _ownedTextures.Add(colorTextureId);
        _ownedTextures.Add(depthTextureId);
        _ownedFrameBuffers.Add(frameBufferId);

        return new GlFrameBufferHandle
        {
            FrameBufferId = frameBufferId,
            ColorTextureId = colorTextureId,
            ImageId = imageId,
            Width = width,
            Height = height,
        };
    }

    /// <summary>The texture and size of the image <paramref name="imageId"/> names, or false when there is none by that id.</summary>
    public bool TryGetImage(string imageId, out uint textureId, out Size size)
    {
        size = default;
        return _textureIdByImageId.TryGetValue(imageId, out textureId) && _sizeByImageId.TryGetValue(imageId, out size);
    }

    public bool HasImage(string imageId) => _textureIdByImageId.ContainsKey(imageId);

    /// <summary>Shared textures are stored top row first, so they are drawn with flipped UVs.</summary>
    public Vector4 GetSrcUv(string imageId) =>
        OperatingSystem.IsWindows() && _sharedByImageId.ContainsKey(imageId) ? new Vector4(0f, 1f, 1f, -1f) : new Vector4(0f, 0f, 1f, 1f);

    public bool SupportsSharedTextures(SharedTextureKind kind) => kind switch
    {
        SharedTextureKind.D3D11NtHandle => GetBridge() is SharedTextureBridge.Created,
        SharedTextureKind.IOSurface => false,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };

    /// <summary>
    /// Copies <paramref name="texture"/> into the image under <paramref name="imageId"/>, creating or
    /// resizing it as needed. Needs a context of this manager's share group current.
    /// </summary>
    public bool CreateOrUpdateSharedTextureImage(string imageId, in SharedTexture texture)
    {
        switch (texture.Kind)
        {
            case SharedTextureKind.D3D11NtHandle:
                return OperatingSystem.IsWindows() && CopyD3D11Texture(imageId, texture.Handle);
            case SharedTextureKind.IOSurface:
                return false;
            default:
                throw new ArgumentOutOfRangeException(nameof(texture), texture.Kind, null);
        }
    }

    [SupportedOSPlatform("windows")]
    private bool CopyD3D11Texture(string imageId, nint ntHandle)
    {
        if (GetBridge() is not SharedTextureBridge.Created created)
            return false;

        _sharedByImageId.TryGetValue(imageId, out var current);
        if (!created.Bridge.TryCopy(ntHandle, current, out var target, out var failure))
        {
            Console.Error.WriteLine($"[GlImageManager] shared texture for '{imageId}': {failure}");
            return false;
        }

        if (!ReferenceEquals(target, current))
        {
            RemoveImage(imageId);
            _sharedByImageId[imageId] = target;
            _textureIdByImageId[imageId] = target.GlTexture;
        }
        _sizeByImageId[imageId] = new Size { Width = target.Width, Height = target.Height };
        return true;
    }

    private SharedTextureBridge GetBridge()
    {
        if (_bridge is not SharedTextureBridge.NotCreated)
            return _bridge;
        if (_getProcAddress == null || !OperatingSystem.IsWindows())
        {
            _bridge = new SharedTextureBridge.Unavailable("no WGL loader");
            return _bridge;
        }
        if (_home.IsNone)
        {
            _bridge = new SharedTextureBridge.Unavailable("no context was current when the image manager was made");
            return _bridge;
        }
        var bridge = D3D11GlBridge.TryCreate(_getProcAddress, _home, out var failure);
        if (bridge == null)
            Console.Error.WriteLine($"[GlImageManager] shared textures unavailable: {failure}");
        _bridge = bridge == null
            ? new SharedTextureBridge.Unavailable(failure)
            : new SharedTextureBridge.Created(bridge);
        return _bridge;
    }

    /// <summary>
    /// Creates or replaces a dynamic image from straight-alpha RGBA8, top-down rows.
    /// The manager flips to the bottom-up convention the canvas_image shader samples with.
    /// An image of this id already uploaded is rewritten in place, so a draw of it staged this frame and not yet
    /// issued shows the new pixels: GL runs the upload before the draws issued after it.
    /// </summary>
    public bool CreateOrUpdateRgbaImage(string imageId, int width, int height, ReadOnlySpan<byte> rgbaTopDown)
    {
        if (OperatingSystem.IsWindows() && _sharedByImageId.ContainsKey(imageId))
            RemoveImage(imageId);
        var flipped = FlipRows(rgbaTopDown, width, height);

        if (_textureIdByImageId.TryGetValue(imageId, out var textureId))
        {
            var prev = _sizeByImageId[imageId];
            var sameSize = (int)prev.Width == width && (int)prev.Height == height;
            glBindTexture(GL_TEXTURE_2D, textureId);
            glPixelStorei(GL_UNPACK_ALIGNMENT, 1);
            fixed (byte* ptr = flipped)
            {
                if (sameSize)
                    glTexSubImage2D(GL_TEXTURE_2D, 0, 0, 0, width, height, GL_RGBA, GL_UNSIGNED_BYTE, ptr);
                else
                    glTexImage2D(GL_TEXTURE_2D, 0, (int)GL_RGBA8, width, height, 0, GL_RGBA, GL_UNSIGNED_BYTE, ptr);
            }
            AssertNoGlError();
            glBindTexture(GL_TEXTURE_2D, 0);
            _sizeByImageId[imageId] = new Size { Width = width, Height = height };
        }
        else
        {
            textureId = UploadRgbaTexture(width, height, flipped);
            _textureIdByImageId.Add(imageId, textureId);
            _sizeByImageId.Add(imageId, new Size { Width = width, Height = height });
            _ownedTextures.Add(textureId);
        }
        return true;
    }

    /// <summary>Forgets the image at once; its texture is deleted once no canvas frame open now can still draw it.</summary>
    public void RemoveImage(string imageId)
    {
        if (!_textureIdByImageId.Remove(imageId, out var textureId))
            return;
        _sizeByImageId.Remove(imageId);
        if (OperatingSystem.IsWindows() && _sharedByImageId.Remove(imageId, out var shared))
        {
            _held.Retire(textureId, new RetiredTexture.Shared(shared));
            return;
        }
        _held.Retire(textureId, new RetiredTexture.Owned(textureId));
    }

    private byte[] _flipScratch = [];

    private ReadOnlySpan<byte> FlipRows(ReadOnlySpan<byte> topDown, int width, int height)
    {
        var rowBytes = width * 4;
        var needed = rowBytes * height;
        if (_flipScratch.Length < needed)
            _flipScratch = new byte[needed];
        for (var y = 0; y < height; y++)
            topDown.Slice(y * rowBytes, rowBytes).CopyTo(_flipScratch.AsSpan((height - 1 - y) * rowBytes, rowBytes));
        return _flipScratch.AsSpan(0, needed);
    }

    private uint UploadRgbaTexture(int width, int height, ReadOnlySpan<byte> rgba)
    {
        uint textureId;
        glGenTextures(1, &textureId);
        glBindTexture(GL_TEXTURE_2D, textureId);
        glPixelStorei(GL_UNPACK_ALIGNMENT, 1);

        fixed (byte* ptr = rgba)
        {
            glTexImage2D(GL_TEXTURE_2D, 0, (int)GL_RGBA8, width, height, 0, GL_RGBA, GL_UNSIGNED_BYTE, ptr);
        }
        AssertNoGlError();

        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, (int)GL_LINEAR);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, (int)GL_NEAREST);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, (int)GL_CLAMP_TO_EDGE);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, (int)GL_CLAMP_TO_EDGE);
        glBindTexture(GL_TEXTURE_2D, 0);
        return textureId;
    }

    private static byte[] DecodeToRgba(IRawPng png)
    {
        var width = (int)png.Ihdr.Width;
        var height = (int)png.Ihdr.Height;
        var src = png.PixelData;
        var bpp = png.Ihdr.GetBytesPerPixel();
        var colorType = png.Ihdr.ColorType;
        var output = new byte[width * height * 4];

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var srcIndex = (y * width + x) * bpp;
                // Flip Y so the texture matches OpenGL's bottom-up convention.
                var dstY = height - 1 - y;
                var dstIndex = (dstY * width + x) * 4;

                byte r = 0, g = 0, b = 0, a = 255;
                switch (colorType)
                {
                    case ColorType.TrueColorWithAlpha:
                        r = src[srcIndex];
                        g = src[srcIndex + 1];
                        b = src[srcIndex + 2];
                        a = src[srcIndex + 3];
                        break;
                    case ColorType.TrueColor:
                        r = src[srcIndex];
                        g = src[srcIndex + 1];
                        b = src[srcIndex + 2];
                        break;
                    case ColorType.GrayscaleWithAlpha:
                        r = g = b = src[srcIndex];
                        a = src[srcIndex + 1];
                        break;
                    case ColorType.Grayscale:
                        r = g = b = src[srcIndex];
                        break;
                    default:
                        throw new NotSupportedException($"PNG ColorType '{colorType}' is not supported.");
                }

                output[dstIndex] = r;
                output[dstIndex + 1] = g;
                output[dstIndex + 2] = b;
                output[dstIndex + 3] = a;
            }
        }

        return output;
    }

    [SupportedOSPlatform("windows")]
    private void DisposeShared()
    {
        foreach (var shared in _sharedByImageId.Values)
            shared.Dispose();
        _sharedByImageId.Clear();
        if (_bridge is SharedTextureBridge.Created created)
            created.Bridge.Dispose();
    }

    public void Dispose()
    {
        _held.FreeAll();
        if (OperatingSystem.IsWindows())
            DisposeShared();
        _bridge = new SharedTextureBridge.Unavailable("disposed");

        foreach (var fb in _ownedFrameBuffers)
        {
            var id = fb;
            glDeleteFramebuffers(1, &id);
        }
        _ownedFrameBuffers.Clear();

        foreach (var tex in _ownedTextures)
        {
            var id = tex;
            glDeleteTextures(1, &id);
        }
        _ownedTextures.Clear();
        _textureIdByImageId.Clear();
        _sizeByImageId.Clear();
    }
}

internal abstract record SharedTextureBridge
{
    public sealed record NotCreated : SharedTextureBridge;
    public sealed record Unavailable(string Reason) : SharedTextureBridge;
    [SupportedOSPlatform("windows")]
    public sealed record Created(D3D11GlBridge Bridge) : SharedTextureBridge;
}
