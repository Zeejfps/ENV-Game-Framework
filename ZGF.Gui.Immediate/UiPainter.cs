using ZGF.Geometry;

namespace ZGF.Gui.Immediate;

/// <summary>
/// Handed to <see cref="Ui.Paint"/> callbacks once layout is final. <see cref="Rect"/> is the
/// element's laid-out rect; every method takes UI-space coordinates (top-left origin, Y down)
/// and draws at the element's depth, inside its clip.
/// </summary>
public sealed class UiPainter
{
    private readonly Ui _ui;
    private readonly RectStyle _rectStyle = new();
    private ICanvas _canvas;
    private float _originX;
    private float _originY;
    private float _canvasHeight;
    private int _z;

    internal UiPainter(Ui ui, ICanvas canvas)
    {
        _ui = ui;
        _canvas = canvas;
    }

    public UiRect Rect { get; private set; }

    /// <summary>The part of the window the element is drawn inside: its scrolls' and clipping
    /// ancestors' visible area, which stays put while a scroll moves <see cref="Rect"/>.</summary>
    public UiRect Clip { get; private set; }

    public int ZIndex => _z;

    internal void Begin(UiRect rect, UiRect clip, int z, ICanvas canvas, float originX, float originY, float canvasHeight)
    {
        Rect = rect;
        Clip = clip;
        _z = z;
        _canvas = canvas;
        _originX = originX;
        _originY = originY;
        _canvasHeight = canvasHeight;
    }

    public void FillRect(UiRect rect, uint color, float radius = 0f, uint borderColor = 0, float borderWidth = 0f)
    {
        _rectStyle.BackgroundColor = color;
        _rectStyle.BorderColor = BorderColorStyle.All(borderColor);
        _rectStyle.BorderSize = BorderSizeStyle.All(borderWidth);
        _rectStyle.BorderRadius = BorderRadiusStyle.All(radius);
        _canvas.DrawRect(new DrawRectInputs { Position = ToCanvas(rect), Style = _rectStyle, ZIndex = _z });
    }

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
        _canvas.DrawRect(new DrawRectInputs { Position = ToCanvas(rect), Style = _rectStyle, ZIndex = _z });
    }

    public void Line(PointF from, PointF to, float thickness, uint color) =>
        _canvas.DrawLine(new DrawLineInputs { Start = ToCanvas(from), End = ToCanvas(to), Thickness = thickness, Color = color, ZIndex = _z });

    /// <summary>A filled disc, or a ring when <paramref name="thickness"/> is positive.</summary>
    public void Circle(PointF center, float radius, uint color, float thickness = 0f) =>
        _canvas.DrawCircle(new DrawCircleInputs { Center = ToCanvas(center), Radius = radius, Color = color, Thickness = thickness, ZIndex = _z });

    /// <summary>Draws <paramref name="text"/> vertically centered in <paramref name="rect"/>. A null
    /// <paramref name="family"/> means the theme's <see cref="UiTheme.FontFamily"/>.</summary>
    /// <summary>Draws the image <paramref name="imageId"/>, set with <see cref="Ui.SetImage"/>,
    /// fitted into <paramref name="rect"/> keeping its aspect and multiplied by
    /// <paramref name="tint"/>; nothing when there is no such image.</summary>
    public void Image(UiRect rect, string imageId, uint tint = 0xFFFFFFFF)
    {
        if (_canvas.HasImage(imageId))
            _canvas.DrawImage(new DrawImageInputs { Position = ToCanvas(rect), ImageId = imageId, TintColor = tint, Rotation = 0f, ZIndex = _z });
    }

    public void Text(UiRect rect, string text, uint color, float? size = null, bool bold = false, Align align = Align.Start, string? family = null)
    {
        if (string.IsNullOrEmpty(text)) return;
        _canvas.DrawText(new DrawTextInputs { Position = ToCanvas(rect), Text = text, Style = _ui.TextStyleFor(size, color, bold, align, family), ZIndex = _z });
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
            Position = ToCanvas(rect),
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
            Origin = ToCanvas(origin),
            CodePoints = codePoints,
            CellAdvance = cellAdvance,
            Style = _ui.TextStyleFor(size, color, bold, Align.Start, family),
            ZIndex = _z,
            Underline = underline,
        });
    }

    private RectF ToCanvas(UiRect rect) => new(rect.X - _originX, _canvasHeight - (rect.Y - _originY) - rect.Height, rect.Width, rect.Height);

    private PointF ToCanvas(PointF point) => new(point.X - _originX, _canvasHeight - (point.Y - _originY));
}
