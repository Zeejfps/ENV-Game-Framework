namespace ZGF.Desktop;

/// <summary>A window's icon at several sizes; the platform draws the one closest to each place it
/// shows the icon, such as the title bar and the taskbar.</summary>
public sealed record WindowIcon(IReadOnlyList<WindowIconImage> Images)
{
    /// <summary>Reads the packed icon format: an image count, then for each image its width, its
    /// height and its RGBA pixels, the numbers as little-endian 32-bit integers.</summary>
    public static WindowIcon FromPacked(byte[] packed)
    {
        var count = BitConverter.ToInt32(packed, 0);
        var images = new List<WindowIconImage>(count);
        var offset = 4;
        for (var i = 0; i < count; i++)
        {
            var width = BitConverter.ToInt32(packed, offset);
            var height = BitConverter.ToInt32(packed, offset + 4);
            offset += 8;
            var pixels = packed.AsSpan(offset, width * height * 4).ToArray();
            offset += pixels.Length;
            images.Add(new WindowIconImage(width, height, pixels));
        }
        return new WindowIcon(images);
    }
}
