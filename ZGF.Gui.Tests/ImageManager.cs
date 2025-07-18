using OpenGL.NET;
using PngSharp.Api;
using static GL46;
using static OpenGLSandbox.OpenGlUtils;
using static OpenGL.NET.GLTexture;

namespace ZGF.Gui.Tests;

sealed class GpuImage
{
    public required uint TextureId { get; init; }
    public required int Width { get; init; }
    public required int Height { get; init; }
}

public sealed class ImageManager : IImageManager
{
    private readonly Dictionary<string, GpuImage> _imageByUriLookup = new();

    public void LoadImage(string imageUri)
    {
        var png = Png.DecodeFromFile(imageUri);

        var texture = new Texture2DBuilder()
            .WithMagFilter(TextureMagFilter.Nearest)
            .WithMinFilter(TextureMinFilter.Nearest)
            .BindAndBuild();

        glTexImage2D<byte>(texture, 0,
            GL_RGBA8,
            png.Width,
            png.Height,
            GL_RGBA,
            GL_BYTE,
            png.PixelData);
        AssertNoGlError();

        var gpuImage = new GpuImage
        {
            TextureId = texture.Id,
            Width = png.Width,
            Height = png.Height,
        };

        _imageByUriLookup.Add(imageUri, gpuImage);
    }

    public int GetImageWidth(string imageUri)
    {
        return _imageByUriLookup[imageUri].Width;
    }

    public int GetImageHeight(string imageUri)
    {
        return _imageByUriLookup[imageUri].Height;
    }
}