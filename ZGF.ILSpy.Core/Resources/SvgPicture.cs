using System.Text;
using ZGF.Svg;

namespace ZGF.ILSpy.Core.Resources;

/// <summary>An SVG resource drawn to pixels at its own size by ZGF.Svg, shrunk to fit
/// <see cref="MaxSide"/> when larger.</summary>
internal static class SvgPicture
{
    private const string Format = "SVG";
    private const string SvgElement = "<svg";
    private const int MaxSide = 2048;
    private const int FallbackSide = 256;

    /// <summary>Whether <paramref name="data"/> is markup with an <c>svg</c> element.</summary>
    public static bool IsSvg(byte[] data)
    {
        var text = ReadText(data);
        var trimmed = text.TrimStart();
        if (!trimmed.StartsWith('<'))
            return false;
        return trimmed.Contains(SvgElement, StringComparison.Ordinal);
    }

    public static ResourceImage Rasterize(byte[] data)
    {
        var document = SvgDocument.Parse(ReadText(data));
        var (width, height) = PixelSize(document);
        var rgba = document.Rasterize(width, height);
        return new ResourceImage(width, height, rgba, Format);
    }

    private static string ReadText(byte[] data)
    {
        using var reader = new StreamReader(new MemoryStream(data), Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
    }

    /// <summary>The document's own size in whole pixels, shrunk evenly to fit
    /// <see cref="MaxSide"/>; a square of <see cref="FallbackSide"/> when it states none.</summary>
    private static (int Width, int Height) PixelSize(SvgDocument document)
    {
        var width = document.IntrinsicWidth;
        var height = document.IntrinsicHeight;
        if (width <= 0f || height <= 0f)
            return (FallbackSide, FallbackSide);
        var scale = MathF.Min(1f, MaxSide / MathF.Max(width, height));
        return (WholePixels(width * scale), WholePixels(height * scale));
    }

    private static int WholePixels(float size) => Math.Max(1, (int)MathF.Ceiling(size));
}
