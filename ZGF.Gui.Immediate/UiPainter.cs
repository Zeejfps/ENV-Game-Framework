using ZGF.Geometry;

namespace ZGF.Gui.Immediate;

/// <summary>
/// Handed to <see cref="Ui.Paint"/> callbacks once layout is final. <see cref="Rect"/> is the
/// element's laid-out rect; every method takes UI-space coordinates (top-left origin, Y down)
/// and draws at the element's depth, inside its clip.
/// </summary>
public sealed class UiPainter
{
    private const float GlyphHalf = 3.5f;
    private const float FadeStep = 2f;

    private readonly Ui _ui;
    private readonly RectStyle _rectStyle = new();
    private ICanvas _canvas;
    private CanvasOrigin _origin;
    private int _z;

    internal UiPainter(Ui ui, ICanvas canvas)
    {
        _ui = ui;
        _canvas = canvas;
    }

    public UiRect Rect { get; private set; }

    /// <summary>The part of the window the element is drawn inside: its scrolls' and clipping
    /// ancestors' visible area, and its own when it clips, which stays put while a scroll moves
    /// <see cref="Rect"/>.</summary>
    public UiRect Clip { get; private set; }

    public int ZIndex => _z;

    internal void Begin(UiRect rect, UiRect clip, int z, ICanvas canvas, CanvasOrigin origin)
    {
        Rect = rect;
        Clip = clip;
        _z = z;
        _canvas = canvas;
        _origin = origin;
    }

    public void FillRect(UiRect rect, uint color, float radius = 0f, uint borderColor = 0, float borderWidth = 0f) =>
        FillRect(rect, color, new Corners(radius), borderColor, borderWidth);

    public void FillRect(UiRect rect, uint color, Corners radii, uint borderColor = 0, float borderWidth = 0f)
    {
        _rectStyle.BackgroundColor = color;
        _rectStyle.BorderColor = BorderColorStyle.All(borderColor);
        _rectStyle.BorderSize = BorderSizeStyle.All(borderWidth);
        _rectStyle.BorderRadius = new BorderRadiusStyle
        {
            TopLeft = radii.TopLeft,
            TopRight = radii.TopRight,
            BottomRight = radii.BottomRight,
            BottomLeft = radii.BottomLeft,
        };
        _canvas.DrawRect(new DrawRectInputs { Position = _origin.ToCanvas(rect), Style = _rectStyle, ZIndex = _z });
    }

    /// <summary>Fills <paramref name="rect"/> with <paramref name="color"/> fading sideways, from
    /// clear at one edge to solid at the <paramref name="solid"/> edge.</summary>
    public void FillFade(UiRect rect, uint color, Align solid)
    {
        var clear = color & 0x00FFFFFFu;
        var steps = (int)MathF.Ceiling(rect.Width / FadeStep);
        for (var i = 0; i < steps; i++)
        {
            var x = rect.X + i * FadeStep;
            var slice = new UiRect(x, rect.Y, MathF.Min(FadeStep, rect.Right - x), rect.Height);
            var opacity = solid == Align.End ? (i + 1f) / steps : 1f - (float)i / steps;
            FillRect(slice, UiColor.Mix(clear, color, opacity));
        }
    }

    public void Line(PointF from, PointF to, float thickness, uint color) =>
        _canvas.DrawLine(new DrawLineInputs { Start = _origin.ToCanvas(from), End = _origin.ToCanvas(to), Thickness = thickness, Color = color, ZIndex = _z });

    /// <summary>A line of dashes <paramref name="dashLength"/> long with gaps of <paramref name="gapLength"/>
    /// between them, from <paramref name="from"/>.</summary>
    public void DashedLine(PointF from, PointF to, float thickness, uint color, float dashLength, float gapLength)
    {
        _canvas.DrawLine(new DrawLineInputs
        {
            Start = _origin.ToCanvas(from),
            End = _origin.ToCanvas(to),
            Thickness = thickness,
            Color = color,
            ZIndex = _z,
            DashLength = dashLength,
            GapLength = gapLength,
        });
    }

    /// <summary>A cubic Bézier curve from <paramref name="start"/> to <paramref name="end"/>, drawn toward
    /// <paramref name="control1"/> and <paramref name="control2"/>; dashed as <see cref="DashedLine"/> is
    /// when <paramref name="dashLength"/> and <paramref name="gapLength"/> are positive.</summary>
    public void CubicBezier(PointF start, PointF control1, PointF control2, PointF end, float thickness, uint color, float dashLength = 0f,
        float gapLength = 0f)
    {
        _canvas.DrawCubicBezier(new DrawCubicBezierInputs
        {
            Start = _origin.ToCanvas(start),
            Control1 = _origin.ToCanvas(control1),
            Control2 = _origin.ToCanvas(control2),
            End = _origin.ToCanvas(end),
            Thickness = thickness,
            Color = color,
            ZIndex = _z,
            DashLength = dashLength,
            GapLength = gapLength,
        });
    }

    /// <summary>The outline of a triangle pointing at <paramref name="tip"/> from the side of
    /// <paramref name="tail"/>, <paramref name="length"/> long and <paramref name="width"/> wide at its
    /// base, as UML draws inheritance.</summary>
    public void HollowArrowhead(PointF tip, PointF tail, float length, float width, float thickness, uint color)
    {
        var corners = ArrowCorners.Of(tip, tail, length, width);
        Line(tip, corners.Left, thickness, color);
        Line(corners.Left, corners.Right, thickness, color);
        Line(corners.Right, tip, thickness, color);
    }

    /// <summary>The two strokes of an open arrowhead pointing at <paramref name="tip"/> from the side of
    /// <paramref name="tail"/>, reaching back <paramref name="length"/> and spreading <paramref name="width"/>
    /// apart, as UML draws an association.</summary>
    public void OpenArrowhead(PointF tip, PointF tail, float length, float width, float thickness, uint color)
    {
        var corners = ArrowCorners.Of(tip, tail, length, width);
        Line(corners.Left, tip, thickness, color);
        Line(corners.Right, tip, thickness, color);
    }

    /// <summary>The outline of a diamond with a point at <paramref name="tip"/> and its far point
    /// <paramref name="length"/> back toward <paramref name="tail"/>, <paramref name="width"/> wide at its
    /// middle, as UML draws aggregation.</summary>
    public void HollowDiamond(PointF tip, PointF tail, float length, float width, float thickness, uint color)
    {
        var middle = ArrowCorners.Of(tip, tail, length / 2, width);
        var back = ArrowCorners.BackOf(tip, tail, length);
        Line(tip, middle.Left, thickness, color);
        Line(middle.Left, back, thickness, color);
        Line(back, middle.Right, thickness, color);
        Line(middle.Right, tip, thickness, color);
    }

    /// <summary>A filled disc, or a ring when <paramref name="thickness"/> is positive.</summary>
    public void Circle(PointF center, float radius, uint color, float thickness = 0f) =>
        _canvas.DrawCircle(new DrawCircleInputs { Center = _origin.ToCanvas(center), Radius = radius, Color = color, Thickness = thickness, ZIndex = _z });

    /// <summary>Draws <paramref name="glyph"/> centered in <see cref="Rect"/>, its lines
    /// <paramref name="stroke"/> wide.</summary>
    public void DrawGlyph(UiGlyph glyph, uint color, float stroke)
    {
        var center = new PointF(Rect.X + Rect.Width / 2, Rect.Y + Rect.Height / 2);
        switch (glyph)
        {
            case UiGlyph.Checkmark:
                DrawCheckmark(Rect, color, stroke);
                break;
            case UiGlyph.Cross:
                DrawCross(center, GlyphHalf, color, stroke);
                break;
            case UiGlyph.DisclosureClosed:
                DrawDisclosure(center, GlyphHalf, false, color, stroke);
                break;
            case UiGlyph.DisclosureOpen:
                DrawDisclosure(center, GlyphHalf, true, color, stroke);
                break;
            case UiGlyph.DropdownArrow:
                DrawDropdownArrow(Rect, color, stroke);
                break;
        }
    }

    public void DrawCheckmark(UiRect r, uint color, float stroke)
    {
        var start = new PointF(r.X + r.Width * 0.25f, r.Y + r.Height * 0.5f);
        var corner = new PointF(r.X + r.Width * 0.42f, r.Y + r.Height * 0.68f);
        var end = new PointF(r.X + r.Width * 0.75f, r.Y + r.Height * 0.32f);
        Line(start, corner, stroke, color);
        Line(corner, end, stroke, color);
    }

    /// <summary>An X reaching <paramref name="half"/> from <paramref name="center"/> each way.</summary>
    public void DrawCross(PointF center, float half, uint color, float stroke)
    {
        var (cx, cy) = (center.X, center.Y);
        Line(new PointF(cx - half, cy - half), new PointF(cx + half, cy + half), stroke, color);
        Line(new PointF(cx - half, cy + half), new PointF(cx + half, cy - half), stroke, color);
    }

    /// <summary>A chevron pointing right, or down when <paramref name="expanded"/>, reaching
    /// <paramref name="half"/> from <paramref name="center"/> along its length.</summary>
    public void DrawDisclosure(PointF center, float half, bool expanded, uint color, float stroke)
    {
        var (cx, cy) = (center.X, center.Y);
        if (expanded)
        {
            Line(new PointF(cx - half, cy - half / 2), new PointF(cx, cy + half / 2), stroke, color);
            Line(new PointF(cx, cy + half / 2), new PointF(cx + half, cy - half / 2), stroke, color);
            return;
        }
        Line(new PointF(cx - half / 2, cy - half), new PointF(cx + half / 2, cy), stroke, color);
        Line(new PointF(cx + half / 2, cy), new PointF(cx - half / 2, cy + half), stroke, color);
    }

    /// <summary>A solid down triangle <paramref name="width"/> wide, centered in <paramref name="r"/>.</summary>
    public void DrawDropdownArrow(UiRect r, uint color, float width)
    {
        var half = width / 2;
        var top = MathF.Round(r.Y + (r.Height - half) / 2);
        var cx = r.X + r.Width / 2;
        var apex = new PointF(cx, top + half);
        for (var x = -half; x <= half; x += 0.5f)
            Line(new PointF(cx + x, top), apex, 1f, color);
    }

    /// <summary>Draws the image <paramref name="imageId"/>, set with <see cref="Ui.SetImage"/>,
    /// fitted into <paramref name="rect"/> keeping its aspect and multiplied by
    /// <paramref name="tint"/>; nothing when there is no such image.</summary>
    public void Image(UiRect rect, string imageId, uint tint = 0xFFFFFFFF)
    {
        if (_canvas.HasImage(imageId))
            _canvas.DrawImage(new DrawImageInputs { Position = _origin.ToCanvas(rect), ImageId = imageId, TintColor = tint, Rotation = 0f, ZIndex = _z });
    }

    /// <summary>Draws <paramref name="glyph"/> at <paramref name="size"/> centered in <paramref name="rect"/>,
    /// in <paramref name="color"/>, a picture as <see cref="Ui.Glyph"/> draws one.</summary>
    public void Glyph(UiRect rect, IconGlyph glyph, uint color, float size)
    {
        if (glyph.Svg != null)
        {
            var imageId = _ui.AcquireIconImage(glyph.Svg, size, color);
            Image(Centered(rect, size), imageId);
            return;
        }
        if (glyph.Text != null)
            Text(rect, glyph.Text, color, size, align: Align.Center, family: glyph.Family);
    }

    /// <summary>A square of <paramref name="size"/> centered in <paramref name="rect"/>.</summary>
    internal static UiRect Centered(UiRect rect, float size) =>
        new(rect.X + (rect.Width - size) / 2, rect.Y + (rect.Height - size) / 2, size, size);

    /// <summary>Draws <paramref name="text"/> vertically centered in <paramref name="rect"/>. A null
    /// <paramref name="family"/> means the theme's <see cref="UiTheme.FontFamily"/>.</summary>
    public void Text(UiRect rect, string text, uint color, float? size = null, bool bold = false, Align align = Align.Start, string? family = null)
    {
        if (string.IsNullOrEmpty(text)) return;
        _canvas.DrawText(new DrawTextInputs { Position = _origin.ToCanvas(rect), Text = text, Style = _ui.TextStyleFor(size, color, bold, align, family), ZIndex = _z });
    }

    /// <summary>Draws <paramref name="length"/> characters of <paramref name="text"/> from
    /// <paramref name="start"/>, shaped on their own, as <see cref="Text(UiRect, string, uint, float?, bool, Align, string?)"/>
    /// does — a slice of a longer line, with nothing allocated. See <see cref="Ui.MeasureText(ReadOnlySpan{char}, float?, bool, string?)"/>.</summary>
    public void Text(UiRect rect, string text, int start, int length, uint color, float? size = null, bool bold = false, Align align = Align.Start,
        string? family = null)
    {
        if (length <= 0) return;
        _canvas.DrawText(new DrawTextInputs
        {
            Position = _origin.ToCanvas(rect),
            Text = text,
            Slice = new Range(start, start + length),
            Style = _ui.TextStyleFor(size, color, bold, align, family),
            ZIndex = _z,
        });
    }

    /// <summary>Draws one code point per column of a monospaced grid whose first cell's top-left
    /// is <paramref name="origin"/>, <paramref name="cellAdvance"/> apart — see
    /// <see cref="Ui.MeasureCell"/>. Nothing is shaped and nothing is allocated, so a code or
    /// terminal view can draw slices of its lines directly.</summary>
    public void GlyphRun(PointF origin, ReadOnlySpan<int> codePoints, float cellAdvance, uint color, float? size = null, bool bold = false,
        string? family = null, bool underline = false)
    {
        if (codePoints.IsEmpty) return;
        _canvas.DrawGlyphRun(new DrawGlyphRunInputs
        {
            Origin = _origin.ToCanvas(origin),
            CodePoints = codePoints,
            CellAdvance = cellAdvance,
            Style = _ui.TextStyleFor(size, color, bold, Align.Start, family),
            ZIndex = _z,
            Underline = underline,
        });
    }
}

/// <summary>The corners at the base of an arrowhead: <see cref="Left"/> and <see cref="Right"/> as seen
/// looking from its tail to its tip.</summary>
internal readonly record struct ArrowCorners(PointF Left, PointF Right)
{
    /// <summary>The corners of an arrowhead at <paramref name="tip"/>, <paramref name="length"/> back
    /// toward <paramref name="tail"/> and <paramref name="width"/> apart; pointing down when the tail
    /// is where the tip is.</summary>
    public static ArrowCorners Of(PointF tip, PointF tail, float length, float width)
    {
        var (dx, dy) = DirectionOf(tip, tail);
        var baseX = tip.X - dx * length;
        var baseY = tip.Y - dy * length;
        var half = width / 2;
        var left = new PointF(baseX + dy * half, baseY - dx * half);
        var right = new PointF(baseX - dy * half, baseY + dx * half);
        return new ArrowCorners(left, right);
    }

    /// <summary>The point <paramref name="length"/> back from <paramref name="tip"/> toward
    /// <paramref name="tail"/>; below the tip when the tail is where the tip is.</summary>
    public static PointF BackOf(PointF tip, PointF tail, float length)
    {
        var (dx, dy) = DirectionOf(tip, tail);
        return new PointF(tip.X - dx * length, tip.Y - dy * length);
    }

    /// <summary>The unit vector from <paramref name="tail"/> to <paramref name="tip"/>.</summary>
    private static (float X, float Y) DirectionOf(PointF tip, PointF tail)
    {
        var dx = tip.X - tail.X;
        var dy = tip.Y - tail.Y;
        var length = MathF.Sqrt(dx * dx + dy * dy);
        if (length == 0f)
            return (0f, 1f);
        return (dx / length, dy / length);
    }
}

/// <summary>Where a canvas sits in UI space: its top-left at (<see cref="X"/>, <see cref="Y"/>),
/// <see cref="Height"/> tall, with Y up.</summary>
internal readonly record struct CanvasOrigin(float X, float Y, float Height)
{
    public RectF ToCanvas(UiRect rect) => new(rect.X - X, Height - (rect.Y - Y) - rect.Height, rect.Width, rect.Height);

    public PointF ToCanvas(PointF point) => new(point.X - X, Height - (point.Y - Y));
}
