using System.Globalization;
using System.Numerics;
using System.Xml;
using ZGF.Svg.Scene;

namespace ZGF.Svg.Parsing;

/// <summary>
/// Walks the SVG XML and flattens supported elements into an <see cref="SvgScene"/>.
/// Lenient like browsers: unknown elements and attributes are skipped, malformed
/// values fall back to the inherited/initial value.
/// </summary>
internal static class SvgParser
{
    private static readonly XmlReaderSettings ReaderSettings = new()
    {
        DtdProcessing = DtdProcessing.Ignore,
        IgnoreComments = true,
        IgnoreProcessingInstructions = true,
        IgnoreWhitespace = true,
        XmlResolver = null,
    };

    public static SvgDocument Parse(string svgText)
    {
        var segments = new List<PathSegment>();
        var commands = new List<SvgDrawCommand>();
        var dashValues = new List<float>();
        var styleStack = new Stack<StyleState>();
        var usesCurrentColor = false;
        var viewBox = default(SvgViewBox);
        var hasViewBox = false;
        var widthAttr = -1f;
        var heightAttr = -1f;
        var seenRoot = false;

        using var reader = XmlReader.Create(new StringReader(svgText), ReaderSettings);

        var advance = true;
        while (!reader.EOF)
        {
            if (advance && !reader.Read())
                break;
            advance = true;

            if (reader.NodeType == XmlNodeType.EndElement)
            {
                if (reader.LocalName is "g" or "svg" && styleStack.Count > 0)
                    styleStack.Pop();
                continue;
            }

            if (reader.NodeType != XmlNodeType.Element)
                continue;

            if (!seenRoot)
            {
                if (reader.LocalName != "svg")
                    continue;
                seenRoot = true;

                var rootStyle = StyleState.CreateInitial();
                ApplyAttributes(reader, ref rootStyle, dashValues);
                hasViewBox = TryParseViewBox(reader.GetAttribute("viewBox"), out viewBox);
                TryParseLength(reader.GetAttribute("width"), out widthAttr);
                TryParseLength(reader.GetAttribute("height"), out heightAttr);

                if (reader.IsEmptyElement)
                    break;
                styleStack.Push(rootStyle);
                continue;
            }

            if (styleStack.Count == 0)
                continue;

            var inherited = styleStack.Peek();
            switch (reader.LocalName)
            {
                case "g":
                {
                    if (reader.IsEmptyElement)
                        continue;
                    var style = inherited;
                    if (!ApplyAttributes(reader, ref style, dashValues))
                    {
                        // display:none — nothing inside can render.
                        reader.Skip();
                        advance = false;
                        continue;
                    }
                    styleStack.Push(style);
                    continue;
                }
                case "path":
                case "rect":
                case "circle":
                case "ellipse":
                case "line":
                case "polyline":
                case "polygon":
                {
                    var style = inherited;
                    var visible = ApplyAttributes(reader, ref style, dashValues);
                    if (visible)
                        EmitShape(reader, reader.LocalName, style, segments, commands, dashValues, ref usesCurrentColor);
                    if (!reader.IsEmptyElement)
                    {
                        reader.Skip();
                        advance = false;
                    }
                    continue;
                }
                default:
                    // Unknown or unsupported (defs, title, style, filters, nested svg, ...).
                    reader.Skip();
                    advance = false;
                    continue;
            }
        }

        if (!seenRoot)
            throw new FormatException("No <svg> root element found.");

        if (!hasViewBox)
        {
            if (widthAttr > 0f && heightAttr > 0f)
                viewBox = new SvgViewBox(0f, 0f, widthAttr, heightAttr);
            else
                viewBox = ComputeContentBounds(segments);
        }

        var intrinsicWidth = widthAttr > 0f ? widthAttr : viewBox.Width;
        var intrinsicHeight = heightAttr > 0f ? heightAttr : viewBox.Height;

        var scene = segments.Count == 0 || commands.Count == 0
            ? SvgScene.Empty
            : new SvgScene
            {
                Segments = segments.ToArray(),
                Commands = commands.ToArray(),
                DashValues = dashValues.ToArray(),
            };

        return new SvgDocument(scene, viewBox, intrinsicWidth, intrinsicHeight, usesCurrentColor);
    }

    private static void EmitShape(
        XmlReader reader,
        string name,
        in StyleState style,
        List<PathSegment> segments,
        List<SvgDrawCommand> commands,
        List<float> dashValues,
        ref bool usesCurrentColor)
    {
        var fill = style.ResolveFill();
        var stroke = style.ResolveStroke();
        var strokeVisible = stroke.Kind != SvgPaintKind.None && style.StrokeWidth > 0f;
        if (fill.Kind == SvgPaintKind.None && !strokeVisible)
            return;
        if (!strokeVisible)
            stroke = SvgPaint.None;

        var segStart = segments.Count;
        switch (name)
        {
            case "path":
            {
                var d = reader.GetAttribute("d");
                if (d != null)
                    PathDataParser.Parse(d, segments);
                break;
            }
            case "rect":
                ShapeLowering.AddRect(segments,
                    GetLength(reader, "x"), GetLength(reader, "y"),
                    GetLength(reader, "width"), GetLength(reader, "height"),
                    GetLength(reader, "rx", -1f), GetLength(reader, "ry", -1f));
                break;
            case "circle":
            {
                var r = GetLength(reader, "r");
                ShapeLowering.AddEllipse(segments, GetLength(reader, "cx"), GetLength(reader, "cy"), r, r);
                break;
            }
            case "ellipse":
                ShapeLowering.AddEllipse(segments,
                    GetLength(reader, "cx"), GetLength(reader, "cy"),
                    GetLength(reader, "rx"), GetLength(reader, "ry"));
                break;
            case "line":
                ShapeLowering.AddLine(segments,
                    GetLength(reader, "x1"), GetLength(reader, "y1"),
                    GetLength(reader, "x2"), GetLength(reader, "y2"));
                // A bare line has no interior.
                fill = SvgPaint.None;
                break;
            case "polyline":
            case "polygon":
            {
                var points = reader.GetAttribute("points");
                if (points != null)
                    ShapeLowering.AddPoly(segments, points, close: name == "polygon");
                break;
            }
        }

        var segCount = segments.Count - segStart;
        if (segCount == 0)
            return;
        if (fill.Kind == SvgPaintKind.None && stroke.Kind == SvgPaintKind.None)
        {
            segments.RemoveRange(segStart, segCount);
            return;
        }

        usesCurrentColor |= fill.Kind == SvgPaintKind.CurrentColor || stroke.Kind == SvgPaintKind.CurrentColor;

        commands.Add(new SvgDrawCommand
        {
            SegStart = segStart,
            SegCount = segCount,
            Transform = style.Transform,
            Fill = fill,
            Stroke = stroke,
            FillRule = style.FillRule,
            StrokeWidth = style.StrokeWidth,
            MiterLimit = style.MiterLimit,
            Cap = style.Cap,
            Join = style.Join,
            DashStart = style.DashStart,
            DashCount = style.DashCount,
            DashOffset = style.DashOffset,
        });
    }

    /// <summary>
    /// Applies presentation attributes then the inline style attribute (which wins).
    /// Returns false if the element is display:none.
    /// </summary>
    private static bool ApplyAttributes(XmlReader reader, ref StyleState style, List<float> dashValues)
    {
        var visible = true;
        if (reader.MoveToFirstAttribute())
        {
            string? inlineStyle = null;
            do
            {
                if (reader.LocalName == "style")
                    inlineStyle = reader.Value;
                else
                    visible &= ApplyProperty(reader.LocalName, reader.Value, ref style, dashValues);
            } while (reader.MoveToNextAttribute());
            reader.MoveToElement();

            if (inlineStyle != null)
                visible &= ApplyInlineStyle(inlineStyle, ref style, dashValues);
        }
        return visible;
    }

    private static bool ApplyInlineStyle(string css, ref StyleState style, List<float> dashValues)
    {
        var visible = true;
        var span = css.AsSpan();
        foreach (var declRange in span.Split(';'))
        {
            var decl = span[declRange];
            var colon = decl.IndexOf(':');
            if (colon <= 0)
                continue;
            var property = decl[..colon].Trim();
            var value = decl[(colon + 1)..].Trim();
            visible &= ApplyProperty(property.ToString(), value.ToString(), ref style, dashValues);
        }
        return visible;
    }

    private static bool ApplyProperty(string name, string value, ref StyleState style, List<float> dashValues)
    {
        switch (name)
        {
            case "fill":
                if (ColorParser.TryParsePaint(value, out var fillKind, out var fillArgb))
                {
                    style.FillKind = fillKind;
                    style.FillArgb = fillArgb;
                }
                break;
            case "stroke":
                if (ColorParser.TryParsePaint(value, out var strokeKind, out var strokeArgb))
                {
                    style.StrokeKind = strokeKind;
                    style.StrokeArgb = strokeArgb;
                }
                break;
            case "fill-opacity":
                if (TryParseOpacity(value, out var fo))
                    style.FillOpacity = fo;
                break;
            case "stroke-opacity":
                if (TryParseOpacity(value, out var so))
                    style.StrokeOpacity = so;
                break;
            case "opacity":
                if (TryParseOpacity(value, out var o))
                    style.OpacityProduct *= o;
                break;
            case "fill-rule":
                if (value == "evenodd")
                    style.FillRule = SvgFillRule.EvenOdd;
                else if (value == "nonzero")
                    style.FillRule = SvgFillRule.NonZero;
                break;
            case "stroke-width":
                if (TryParseLength(value, out var sw) && sw >= 0f)
                    style.StrokeWidth = sw;
                break;
            case "stroke-linecap":
                style.Cap = value switch
                {
                    "round" => SvgLineCap.Round,
                    "square" => SvgLineCap.Square,
                    "butt" => SvgLineCap.Butt,
                    _ => style.Cap,
                };
                break;
            case "stroke-linejoin":
                style.Join = value switch
                {
                    "round" => SvgLineJoin.Round,
                    "bevel" => SvgLineJoin.Bevel,
                    "miter" => SvgLineJoin.Miter,
                    _ => style.Join,
                };
                break;
            case "stroke-miterlimit":
                if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var ml) && ml >= 1f)
                    style.MiterLimit = ml;
                break;
            case "stroke-dasharray":
                ApplyDashArray(value, ref style, dashValues);
                break;
            case "stroke-dashoffset":
                if (TryParseLength(value, out var dashOffset))
                    style.DashOffset = dashOffset;
                break;
            case "transform":
                style.Transform = TransformParser.Parse(value) * style.Transform;
                break;
            case "display":
                if (value.Trim() == "none")
                    return false;
                break;
        }
        return true;
    }

    private static void ApplyDashArray(string value, ref StyleState style, List<float> dashValues)
    {
        var trimmed = value.Trim();
        if (trimmed == "none")
        {
            style.DashStart = 0;
            style.DashCount = 0;
            return;
        }

        var start = dashValues.Count;
        var reader = new PathDataReader(trimmed);
        var sum = 0f;
        while (reader.TryReadNumber(out var v))
        {
            if (v < 0f)
            {
                // Per spec, a negative value invalidates the whole list.
                dashValues.RemoveRange(start, dashValues.Count - start);
                return;
            }
            dashValues.Add(v);
            sum += v;
        }

        var count = dashValues.Count - start;
        if (count == 0 || sum == 0f)
        {
            dashValues.RemoveRange(start, dashValues.Count - start);
            style.DashStart = 0;
            style.DashCount = 0;
            return;
        }

        // Odd counts repeat the list, per spec.
        if (count % 2 == 1)
        {
            for (var i = 0; i < count; i++)
                dashValues.Add(dashValues[start + i]);
            count *= 2;
        }

        style.DashStart = start;
        style.DashCount = count;
    }

    private static bool TryParseViewBox(string? value, out SvgViewBox viewBox)
    {
        viewBox = default;
        if (value == null)
            return false;
        var reader = new PathDataReader(value);
        if (reader.TryReadNumber(out var minX) &&
            reader.TryReadNumber(out var minY) &&
            reader.TryReadNumber(out var w) &&
            reader.TryReadNumber(out var h) &&
            w > 0f && h > 0f)
        {
            viewBox = new SvgViewBox(minX, minY, w, h);
            return true;
        }
        return false;
    }

    private static float GetLength(XmlReader reader, string attribute, float fallback = 0f)
    {
        return TryParseLength(reader.GetAttribute(attribute), out var v) ? v : fallback;
    }

    /// <summary>
    /// Parses a length. The px suffix (and other absolute unit suffixes) is
    /// tolerated by ignoring trailing letters; percentages are rejected.
    /// </summary>
    private static bool TryParseLength(string? value, out float result)
    {
        result = 0f;
        if (value == null)
            return false;
        var s = value.AsSpan().Trim();
        if (s.IsEmpty || s.EndsWith("%"))
            return false;
        var end = s.Length;
        while (end > 0 && char.IsAsciiLetter(s[end - 1]))
            end--;
        return float.TryParse(s[..end], NumberStyles.Float, CultureInfo.InvariantCulture, out result);
    }

    private static bool TryParseOpacity(string value, out float result)
    {
        var s = value.AsSpan().Trim();
        var isPercent = s.EndsWith("%");
        if (isPercent)
            s = s[..^1];
        if (!float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out result))
            return false;
        if (isPercent)
            result /= 100f;
        result = Math.Clamp(result, 0f, 1f);
        return true;
    }

    private static SvgViewBox ComputeContentBounds(List<PathSegment> segments)
    {
        if (segments.Count == 0)
            return new SvgViewBox(0f, 0f, 0f, 0f);

        var min = new Vector2(float.MaxValue);
        var max = new Vector2(float.MinValue);
        foreach (var seg in segments)
        {
            if (seg.Kind == SegKind.Close)
                continue;
            if (seg.Kind == SegKind.Cubic)
            {
                min = Vector2.Min(min, Vector2.Min(seg.P1, seg.P2));
                max = Vector2.Max(max, Vector2.Max(seg.P1, seg.P2));
            }
            min = Vector2.Min(min, seg.P3);
            max = Vector2.Max(max, seg.P3);
        }
        return new SvgViewBox(min.X, min.Y, MathF.Max(0f, max.X - min.X), MathF.Max(0f, max.Y - min.Y));
    }
}
