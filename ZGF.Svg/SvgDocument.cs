using System.Text;
using ZGF.Svg.Parsing;
using ZGF.Svg.Scene;

namespace ZGF.Svg;

/// <summary>
/// A parsed, retained SVG document. Parse once, rasterize at any pixel size.
/// Instances are immutable and safe to share across threads; rasterization
/// through a shared <see cref="SvgRasterizer"/> is not.
/// </summary>
public sealed class SvgDocument
{
    internal SvgScene Scene { get; }

    public SvgViewBox ViewBox { get; }
    public float IntrinsicWidth { get; }
    public float IntrinsicHeight { get; }
    public bool UsesCurrentColor { get; }

    internal SvgDocument(SvgScene scene, SvgViewBox viewBox, float intrinsicWidth, float intrinsicHeight, bool usesCurrentColor)
    {
        Scene = scene;
        ViewBox = viewBox;
        IntrinsicWidth = intrinsicWidth;
        IntrinsicHeight = intrinsicHeight;
        UsesCurrentColor = usesCurrentColor;
    }

    public static SvgDocument Parse(string svgText)
    {
        return SvgParser.Parse(svgText);
    }

    public static SvgDocument Parse(ReadOnlySpan<byte> utf8Bytes)
    {
        return Parse(Encoding.UTF8.GetString(utf8Bytes));
    }

    public static bool TryParse(string svgText, out SvgDocument? document, out string? error)
    {
        try
        {
            document = Parse(svgText);
            error = null;
            return true;
        }
        catch (Exception e) when (e is FormatException or System.Xml.XmlException)
        {
            document = null;
            error = e.Message;
            return false;
        }
    }

    /// <summary>One-shot convenience; allocates the buffer and a transient rasterizer.</summary>
    public byte[] Rasterize(int widthPx, int heightPx, uint currentColorArgb = 0xFF000000)
    {
        var buffer = new byte[widthPx * heightPx * 4];
        new SvgRasterizer().Rasterize(this, buffer, widthPx, heightPx, currentColorArgb);
        return buffer;
    }
}
