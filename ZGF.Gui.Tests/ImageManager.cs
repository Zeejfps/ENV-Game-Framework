using PngSharp.Api;

namespace ZGF.Gui.Tests;

public sealed class ImageManager : IImageManager
{
    private readonly Dictionary<string, IDecodedPng> _imageByUriLookup = new();

    public void LoadImage(string imageUri)
    {
        var png = Png.DecodeFromFile(imageUri);
        _imageByUriLookup.Add(imageUri, png);
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