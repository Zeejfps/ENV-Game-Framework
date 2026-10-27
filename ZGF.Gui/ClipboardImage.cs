namespace ZGF.Gui;

/// <summary>A picture read from the clipboard: straight-alpha RGBA8 pixels, rows top-down, as
/// <c>Ui.SetImage</c> takes them.</summary>
public sealed class ClipboardImage
{
    public int Width { get; }
    public int Height { get; }
    public byte[] Rgba { get; }

    public ClipboardImage(int width, int height, byte[] rgba)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        if (rgba.Length != (long)width * height * 4)
            throw new ArgumentException($"A {width}x{height} picture takes {(long)width * height * 4} bytes, not {rgba.Length}.", nameof(rgba));
        Width = width;
        Height = height;
        Rgba = rgba;
    }
}
