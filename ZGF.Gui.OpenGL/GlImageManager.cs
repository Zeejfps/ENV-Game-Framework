using System.Numerics;
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
    private readonly Dictionary<string, D3D11GlTexture> _sharedByImageId = new();
    private readonly GetProcAddressDelegate? _getProcAddress;
    private SharedTextureBridge _bridge = new SharedTextureBridge.NotCreated();

    /// <param name="getProcAddress">Resolves GL and WGL entry points for the shared-texture bridge;
    /// without it the manager imports no shared textures.</param>
    public GlImageManager(GetProcAddressDelegate? getProcAddress = null)
    {
        _getProcAddress = getProcAddress;
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

    public uint GetTextureId(string imageId) => _textureIdByImageId[imageId];
    public Size GetImageSize(string imageId) => _sizeByImageId[imageId];
    public int GetImageWidth(string imageId) => (int)_sizeByImageId[imageId].Width;
    public int GetImageHeight(string imageId) => (int)_sizeByImageId[imageId].Height;

    public bool HasImage(string imageId) => _textureIdByImageId.ContainsKey(imageId);

    /// <summary>Shared textures are stored top row first, so they are drawn with flipped UVs.</summary>
    public Vector4 GetSrcUv(string imageId) =>
        _sharedByImageId.ContainsKey(imageId) ? new Vector4(0f, 1f, 1f, -1f) : new Vector4(0f, 0f, 1f, 1f);

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
                return CopyD3D11Texture(imageId, texture.Handle);
            case SharedTextureKind.IOSurface:
                return false;
            default:
                throw new ArgumentOutOfRangeException(nameof(texture), texture.Kind, null);
        }
    }

    private bool CopyD3D11Texture(string imageId, nint ntHandle)
    {
        if (GetBridge() is not SharedTextureBridge.Created created)
            return false;

        _sharedByImageId.TryGetValue(imageId, out var current);
        var target = current;
        if (!created.Bridge.TryCopy(ntHandle, ref target, out var failure))
        {
            if (current != null && target == null)
                ForgetSharedImage(imageId);
            Console.Error.WriteLine($"[GlImageManager] shared texture for '{imageId}': {failure}");
            return false;
        }

        if (!ReferenceEquals(target, current))
        {
            if (current != null)
                _textureIdByImageId.Remove(imageId);
            else if (_textureIdByImageId.Remove(imageId, out var replaced))
                DeleteOwnedTexture(imageId, replaced);
            _sharedByImageId[imageId] = target!;
            _textureIdByImageId[imageId] = target!.GlTexture;
        }
        _sizeByImageId[imageId] = new Size { Width = target!.Width, Height = target.Height };
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
        var bridge = D3D11GlBridge.TryCreate(_getProcAddress, out var failure);
        if (bridge == null)
            Console.Error.WriteLine($"[GlImageManager] shared textures unavailable: {failure}");
        _bridge = bridge == null
            ? new SharedTextureBridge.Unavailable(failure)
            : new SharedTextureBridge.Created(bridge);
        return _bridge;
    }

    private void ForgetSharedImage(string imageId)
    {
        _sharedByImageId.Remove(imageId);
        _textureIdByImageId.Remove(imageId);
        _sizeByImageId.Remove(imageId);
    }

    private void DeleteOwnedTexture(string imageId, uint textureId)
    {
        _sizeByImageId.Remove(imageId);
        _ownedTextures.Remove(textureId);
        glDeleteTextures(1, &textureId);
    }

    /// <summary>
    /// Creates or replaces a dynamic image from straight-alpha RGBA8, top-down rows.
    /// The manager flips to the bottom-up convention the canvas_image shader samples with.
    /// </summary>
    public bool CreateOrUpdateRgbaImage(string imageId, int width, int height, ReadOnlySpan<byte> rgbaTopDown)
    {
        RemoveSharedImage(imageId);
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

    public void RemoveImage(string imageId)
    {
        if (RemoveSharedImage(imageId))
            return;
        if (!_textureIdByImageId.Remove(imageId, out var textureId))
            return;
        DeleteOwnedTexture(imageId, textureId);
    }

    private bool RemoveSharedImage(string imageId)
    {
        if (!_sharedByImageId.TryGetValue(imageId, out var shared))
            return false;
        ForgetSharedImage(imageId);
        shared.Dispose();
        return true;
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

    public void Dispose()
    {
        foreach (var shared in _sharedByImageId.Values)
            shared.Dispose();
        _sharedByImageId.Clear();
        if (_bridge is SharedTextureBridge.Created created)
            created.Bridge.Dispose();
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
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public sealed record Created(D3D11GlBridge Bridge) : SharedTextureBridge;
}
