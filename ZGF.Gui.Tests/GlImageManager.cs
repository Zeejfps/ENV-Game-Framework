using PngSharp.Api;
using PngSharp.Spec.Chunks.IHDR;
using ZGF.Gui;
using static GL46;
using static OpenGLSandbox.OpenGlUtils;

namespace ZGF.Gui.Tests;

public readonly struct GlFrameBufferHandle
{
    public required uint FrameBufferId { get; init; }
    public required uint ColorTextureId { get; init; }
    public required string ImageId { get; init; }
    public required int Width { get; init; }
    public required int Height { get; init; }
}

public sealed unsafe class GlImageManager : IDisposable
{
    private readonly Dictionary<string, uint> _textureIdByImageId = new();
    private readonly Dictionary<string, Size> _sizeByImageId = new();
    private readonly List<uint> _ownedTextures = new();
    private readonly List<uint> _ownedFrameBuffers = new();

    public void LoadImageFromFile(string pathToImageFile)
    {
        if (_textureIdByImageId.ContainsKey(pathToImageFile))
            return;

        var png = Png.DecodeFromFile(pathToImageFile);
        var rgba = DecodeToRgba(png);

        var textureId = UploadRgbaTexture(png.Width, png.Height, rgba);
        _textureIdByImageId.Add(pathToImageFile, textureId);
        _sizeByImageId.Add(pathToImageFile, new Size { Width = png.Width, Height = png.Height });
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

    private uint UploadRgbaTexture(int width, int height, byte[] rgba)
    {
        uint textureId;
        glGenTextures(1, &textureId);
        glBindTexture(GL_TEXTURE_2D, textureId);
        glPixelStorei(GL_UNPACK_ALIGNMENT, 1);

        fixed (byte* ptr = &rgba[0])
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

    private static byte[] DecodeToRgba(IDecodedPng png)
    {
        var width = png.Width;
        var height = png.Height;
        var src = png.PixelData;
        var bpp = png.BytesPerPixel;
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
                switch (png.ColorType)
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
                        throw new NotSupportedException($"PNG ColorType '{png.ColorType}' is not supported.");
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
