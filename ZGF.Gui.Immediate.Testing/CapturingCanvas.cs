using System.Numerics;
using System.Text;
using ZGF.Geometry;

namespace ZGF.Gui.Immediate.Testing;

/// <summary>Text a frame drew on a surface, where it drew it and the clip around it, in the main window's UI
/// points.</summary>
public sealed record DrawnText(string Text, UiRect Bounds, UiRect? Clip, SurfaceKind Surface);

/// <summary>A real canvas that also records every text and glyph run drawn on it, mapped from the canvas's own
/// space through the transform in effect into the main window's UI space.</summary>
internal sealed class CapturingCanvas(RenderedCanvasBase inner) : ICanvas
{
    private readonly List<DrawnText> _texts = new();
    private readonly Stack<(Vector2 Scale, Vector2 Translation)> _transforms = new();
    private Vector2 _scale = Vector2.One;
    private Vector2 _translation = Vector2.Zero;
    private SurfaceKind _kind = SurfaceKind.Main;
    private UiRect _surface;

    public RenderedCanvasBase Inner => inner;

    public IReadOnlyList<DrawnText> Texts => _texts;

    /// <summary>Forgets what the last frame drew, and maps what this one draws from a canvas whose top-left is
    /// <paramref name="surface"/>'s.</summary>
    public void Reset(SurfaceKind kind, UiRect surface)
    {
        _texts.Clear();
        _transforms.Clear();
        _scale = Vector2.One;
        _translation = Vector2.Zero;
        _kind = kind;
        _surface = surface;
    }

    public void DrawText(in DrawTextInputs inputs)
    {
        if (inputs.Text != null && !inputs.Span.IsEmpty)
            Capture(inputs.Span.ToString(), inputs.Position);
        inner.DrawText(inputs);
    }

    public void DrawGlyphRun(in DrawGlyphRunInputs inputs)
    {
        if (!inputs.CodePoints.IsEmpty)
        {
            var text = new StringBuilder(inputs.CodePoints.Length);
            foreach (var codePoint in inputs.CodePoints)
                text.Append(Rune.TryCreate(codePoint, out var rune) ? rune.ToString() : "�");
            var height = inner.MeasureCellSize(inputs.Style).Height;
            Capture(text.ToString(), new RectF(inputs.Origin.X, inputs.Origin.Y - height, inputs.CellAdvance * inputs.CodePoints.Length, height));
        }
        inner.DrawGlyphRun(inputs);
    }

    private void Capture(string text, RectF position)
    {
        var drawn = new RectF(position.Left * _scale.X + _translation.X, position.Bottom * _scale.Y + _translation.Y,
            position.Width * _scale.X, position.Height * _scale.Y);
        UiRect? clip = inner.TryGetClip(out var clipRect) ? ToUi(clipRect) : null;
        _texts.Add(new DrawnText(text, ToUi(drawn), clip, _kind));
    }

    private UiRect ToUi(RectF rect) => new(_surface.X + rect.Left, _surface.Y + _surface.Height - rect.Top, rect.Width, rect.Height);

    public void PushTranslation(float dx, float dy)
    {
        _transforms.Push((_scale, _translation));
        _translation += new Vector2(dx, dy) * _scale;
        inner.PushTranslation(dx, dy);
    }

    public void PopTranslation()
    {
        PopTransform();
        inner.PopTranslation();
    }

    public void PushScale(float sx, float sy, float pivotX, float pivotY)
    {
        _transforms.Push((_scale, _translation));
        var s = new Vector2(sx, sy);
        _translation += new Vector2(pivotX, pivotY) * (Vector2.One - s) * _scale;
        _scale *= s;
        inner.PushScale(sx, sy, pivotX, pivotY);
    }

    public void PopScale()
    {
        PopTransform();
        inner.PopScale();
    }

    private void PopTransform()
    {
        if (_transforms.Count == 0) return;
        (_scale, _translation) = _transforms.Pop();
    }

    public void DrawRect(in DrawRectInputs inputs) => inner.DrawRect(inputs);
    public void DrawImage(in DrawImageInputs inputs) => inner.DrawImage(inputs);
    public void DrawBoxShadow(in DrawBoxShadowInputs inputs) => inner.DrawBoxShadow(inputs);
    public void DrawLine(in DrawLineInputs inputs) => inner.DrawLine(inputs);
    public void DrawCircle(in DrawCircleInputs inputs) => inner.DrawCircle(inputs);
    public void DrawBezier(in DrawBezierInputs inputs) => inner.DrawBezier(inputs);
    public void DrawCubicBezier(in DrawCubicBezierInputs inputs) => inner.DrawCubicBezier(inputs);
    public bool TryGetClip(out RectF rect) => inner.TryGetClip(out rect);
    public void PushClip(RectF rect) => inner.PushClip(rect);
    public void PushClip(RectF rect, float radius) => inner.PushClip(rect, radius);
    public bool TryGetRoundedClip(out RoundedClip clip) => inner.TryGetRoundedClip(out clip);
    public void PopClip() => inner.PopClip();
    public void PushOpacity(float opacity) => inner.PushOpacity(opacity);
    public void PopOpacity() => inner.PopOpacity();
    public float MeasureTextWidth(ReadOnlySpan<char> text, TextStyle style) => inner.MeasureTextWidth(text, style);
    public float MeasureTextPrefix(ReadOnlySpan<char> text, int prefixLength, TextStyle style) => inner.MeasureTextPrefix(text, prefixLength, style);
    public void MeasureTextPrefixes(ReadOnlySpan<char> text, TextStyle style, Span<float> offsets) => ((ICanvas)inner).MeasureTextPrefixes(text, style, offsets);
    public float MeasureTextLineHeight(TextStyle style) => inner.MeasureTextLineHeight(style);
    public CellMetrics MeasureCellSize(TextStyle style) => inner.MeasureCellSize(style);
    public int GetImageWidth(string imageId) => inner.GetImageWidth(imageId);
    public int GetImageHeight(string imageId) => inner.GetImageHeight(imageId);
    public float DpiScale => inner.DpiScale;
    public bool CreateOrUpdateRgbaImage(string imageId, int widthPx, int heightPx, ReadOnlySpan<byte> rgbaTopDown) =>
        ((ICanvas)inner).CreateOrUpdateRgbaImage(imageId, widthPx, heightPx, rgbaTopDown);
    public bool SupportsSharedTextures(SharedTextureKind kind) => ((ICanvas)inner).SupportsSharedTextures(kind);
    public bool CreateOrUpdateSharedTextureImage(string imageId, in SharedTexture texture) =>
        ((ICanvas)inner).CreateOrUpdateSharedTextureImage(imageId, texture);
    public bool HasImage(string imageId) => ((ICanvas)inner).HasImage(imageId);
    public void RemoveImage(string imageId) => ((ICanvas)inner).RemoveImage(imageId);
}
