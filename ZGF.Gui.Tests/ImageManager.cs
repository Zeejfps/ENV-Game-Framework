using PngSharp.Api;
using PngSharp.Spec.Chunks.IHDR;
using SoftwareRendererModule;
using static GL46;
using static OpenGLSandbox.OpenGlUtils;

namespace ZGF.Gui.Tests;

public readonly struct FrameBufferHandle
{
    public required uint FrameBufferId { get; init; }
    public required string ImageId { get; init; }
    public required Bitmap Bitmap { get; init; }
} 

public sealed unsafe class ImageManager
{
    private readonly Dictionary<string, Bitmap> _imageByIdLookup = new();
    private readonly List<FrameBufferHandle> _framebufferHandles = new();
    
    public void LoadImageFromFile(string pathToImageFile)
    {
        var png = Png.DecodeFromFile(pathToImageFile);
        var bitmap = PngToBitmap(png);
        _imageByIdLookup.Add(pathToImageFile, bitmap);
    }

    public FrameBufferHandle CreateFrameBufferImage(int width, int height)
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
        glFramebufferTexture2D(GL_FRAMEBUFFER, GL_COLOR_ATTACHMENT0, GL_TEXTURE_2D, colorTextureId, 0);
        AssertNoGlError();
        
        uint depthTexture;
        glGenTextures(1, &depthTexture);
        glBindTexture(GL_TEXTURE_2D, depthTexture);
        glTexImage2D(GL_TEXTURE_2D, 0, (int)GL_DEPTH_COMPONENT24, width, height, 0, GL_DEPTH_COMPONENT, GL_FLOAT, (void*)0);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, (int)GL_NEAREST);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, (int)GL_NEAREST);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, (int)GL_CLAMP_TO_EDGE);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, (int)GL_CLAMP_TO_EDGE);
        glFramebufferTexture2D(GL_FRAMEBUFFER, GL_DEPTH_ATTACHMENT, GL_TEXTURE_2D, depthTexture, 0);
        AssertNoGlError();

        var attachment = GL_COLOR_ATTACHMENT0;
        glDrawBuffers(1, &attachment);
        AssertNoGlError();

        if (glCheckFramebufferStatus(GL_FRAMEBUFFER) != GL_FRAMEBUFFER_COMPLETE) {
            throw new Exception("Framebuffer not complete");
        }

        glBindFramebuffer(GL_FRAMEBUFFER, 0);
        AssertNoGlError();
        
        var imageId = $"framebuffer_{frameBufferId}";
        var bitmap = new Bitmap(width, height);
        var handle = new FrameBufferHandle
        {
            ImageId = imageId,
            FrameBufferId = frameBufferId,
            Bitmap = bitmap
        };

        _framebufferHandles.Add(handle);
        _imageByIdLookup.Add(imageId, bitmap);
        return handle;
    }
    
    public void RenderFrameBuffersToBitmaps()
    {
        foreach (var framebufferHandle in _framebufferHandles)
        {
            var bmp = framebufferHandle.Bitmap;
            var bmpWidth = bmp.Width;
            var bmpHeight = bmp.Height;
            glBindFramebuffer(GL_READ_FRAMEBUFFER, framebufferHandle.FrameBufferId);
            glPixelStorei(GL_PACK_ALIGNMENT, 1);
            fixed (uint* ptr = &bmp.Pixels[0])
                glReadPixels(0, 0, bmpWidth, bmpHeight, GL_BGRA, GL_UNSIGNED_BYTE, ptr);
        }
    }

    public void UnloadFrameBufferImages()
    {
        foreach (var framebufferHandle in _framebufferHandles)
        {
            var frameBufferId = framebufferHandle.FrameBufferId;
            if (frameBufferId != 0)
            {
                glDeleteFramebuffers(1, &frameBufferId);
            }
        }
    }
    
    private Bitmap PngToBitmap(IDecodedPng png)
    {
        var width = png.Width;
        var height = png.Height;
        var bitmapPixelData = new uint[width * height];
        var pngPixelData = png.PixelData;
        var bytesPerPixel = png.BytesPerPixel;

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var sourceIndex = (y * width + x) * bytesPerPixel;

                var destY = height - 1 - y;
                var destIndex = destY * width + x;

                byte r = 0, g = 0, b = 0;
                byte a = 255; // Default to fully opaque.

                switch (png.ColorType)
                {
                    case ColorType.TrueColorWithAlpha: // 4 bytes: R, G, B, A
                        r = pngPixelData[sourceIndex];
                        g = pngPixelData[sourceIndex + 1];
                        b = pngPixelData[sourceIndex + 2];
                        a = pngPixelData[sourceIndex + 3];
                        break;

                    case ColorType.TrueColor: // 3 bytes: R, G, B
                        r = pngPixelData[sourceIndex];
                        g = pngPixelData[sourceIndex + 1];
                        b = pngPixelData[sourceIndex + 2];
                        break;

                    case ColorType.GrayscaleWithAlpha: // 2 bytes: Greyscale, Alpha
                        r = g = b = pngPixelData[sourceIndex];
                        a = pngPixelData[sourceIndex + 1];
                        break;

                    case ColorType.Grayscale: // 1 byte: Greyscale
                        r = g = b = pngPixelData[sourceIndex];
                        break;

                    // Note: Indexed ColorType would require a PLTE chunk lookup.
                    // This implementation assumes the PngSharp library has already
                    // resolved indexed colors into one of the above formats.
                    default:
                        throw new NotSupportedException($"The PNG ColorType '{png.ColorType}' is not supported.");
                }

                bitmapPixelData[destIndex] = (uint)((a << 24) | (r << 16) | (g << 8) | b);
            }
        }

        return new Bitmap(width, height, bitmapPixelData);
    }

    public int GetImageWidth(string imageUri)
    {
        return _imageByIdLookup[imageUri].Width;
    }

    public int GetImageHeight(string imageUri)
    {
        return _imageByIdLookup[imageUri].Height;
    }

    public Bitmap GetImageId(string imageId)
    {
        return _imageByIdLookup[imageId];
    }

    public Size GetImageSize(string imageId)
    {
        var bitmap = _imageByIdLookup[imageId];
        return new Size
        {
            Width = bitmap.Width,
            Height = bitmap.Height,
        };
    }
}