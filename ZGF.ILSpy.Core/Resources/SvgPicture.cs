using System.Text;
using ZGF.Svg;

namespace ZGF.ILSpy.Core.Resources;

/// <summary>An SVG resource drawn to pixels by ZGF.Svg: at its own size first, shrunk to fit
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
        var (width, height) = OwnSize(document);
        return Rasterize(document, width, height);
    }

    /// <summary><paramref name="document"/> drawn <paramref name="width"/> by
    /// <paramref name="height"/>, shrunk evenly to fit <see cref="MaxSide"/>.</summary>
    public static ResourceImage Rasterize(SvgDocument document, float width, float height)
    {
        var scale = MathF.Min(1f, MaxSide / MathF.Max(width, height));
        var widthPx = WholePixels(width * scale);
        var heightPx = WholePixels(height * scale);
        var rgba = document.Rasterize(widthPx, heightPx);
        return new ResourceImage(widthPx, heightPx, rgba, Format, document);
    }

    private static string ReadText(byte[] data)
    {
        using var reader = new StreamReader(new MemoryStream(data), Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
    }

    /// <summary>The size the document states, or a square of <see cref="FallbackSide"/> when it
    /// states none.</summary>
    private static (float Width, float Height) OwnSize(SvgDocument document)
    {
        var width = document.IntrinsicWidth;
        var height = document.IntrinsicHeight;
        if (width <= 0f || height <= 0f)
            return (FallbackSide, FallbackSide);
        return (width, height);
    }

    private static int WholePixels(float size) => Math.Max(1, (int)MathF.Ceiling(size));
}
