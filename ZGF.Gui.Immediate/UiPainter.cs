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

    /// <summary>The canvas the element is drawn on — the main window's, or a popup's — for drawing
    /// the painter has no method for. Convert coordinates with <see cref="ToCanvas(UiRect)"/>.</summary>
    public ICanvas Canvas => _canvas;

    public int ZIndex => _z;

    internal void Begin(UiRect rect, int z, ICanvas canvas, float originX, float originY, float canvasHeight)
    {
        Rect = rect;
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

    public void Line(PointF from, PointF to, float thickness, uint color) =>
        _canvas.DrawLine(new DrawLineInputs { Start = ToCanvas(from), End = ToCanvas(to), Thickness = thickness, Color = color, ZIndex = _z });

    /// <summary>A filled disc, or a ring when <paramref name="thickness"/> is positive.</summary>
    public void Circle(PointF center, float radius, uint color, float thickness = 0f) =>
        _canvas.DrawCircle(new DrawCircleInputs { Center = ToCanvas(center), Radius = radius, Color = color, Thickness = thickness, ZIndex = _z });

    /// <summary>Draws <paramref name="text"/> vertically centered in <paramref name="rect"/>. A null
    /// <paramref name="family"/> means the style's <see cref="UiStyle.FontFamily"/>.</summary>
    public void Text(UiRect rect, string text, uint color, float? size = null, bool bold = false, Align align = Align.Start, string? family = null)
    {
        if (string.IsNullOrEmpty(text)) return;
        _canvas.DrawText(new DrawTextInputs { Position = ToCanvas(rect), Text = text, Style = _ui.TextStyleFor(size, color, bold, align, family), ZIndex = _z });
    }

    public RectF ToCanvas(UiRect rect) => new(rect.X - _originX, _canvasHeight - (rect.Y - _originY) - rect.Height, rect.Width, rect.Height);

    public PointF ToCanvas(PointF point) => new(point.X - _originX, _canvasHeight - (point.Y - _originY));
}
