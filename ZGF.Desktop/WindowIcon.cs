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

    /// <summary>The smallest image at least <paramref name="pixels"/> wide, or else the largest; null
    /// when the icon has none.</summary>
    public WindowIconImage? PickImage(int pixels)
    {
        WindowIconImage? picked = null;
        foreach (var image in Images)
        {
            if (IsBetterPick(image, picked, pixels))
                picked = image;
        }
        return picked;
    }

    private static bool IsBetterPick(WindowIconImage image, WindowIconImage? picked, int pixels)
    {
        if (picked == null)
            return true;
        var current = picked.Value.Width;
        if (current < pixels)
            return image.Width > current;
        return image.Width >= pixels && image.Width < current;
    }
}
