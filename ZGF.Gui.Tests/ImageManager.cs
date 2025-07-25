using PngSharp.Api;
using PngSharp.Spec.Chunks.IHDR;
using SoftwareRendererModule;

namespace ZGF.Gui.Tests;

public sealed class ImageManager : IImageManager
{
    private readonly Dictionary<string, Bitmap> _imageByUriLookup = new();

    public void LoadImageFromFile(string pathToImageFile)
    {
        var png = Png.DecodeFromFile(pathToImageFile);
        var bitmap = PngToBitmap(png);
        _imageByUriLookup.Add(pathToImageFile, bitmap);
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
        return _imageByUriLookup[imageUri].Width;
    }

    public int GetImageHeight(string imageUri)
    {
        return _imageByUriLookup[imageUri].Height;
    }

    public Bitmap GetImage(string imageUri)
    {
        return _imageByUriLookup[imageUri];
    }
}