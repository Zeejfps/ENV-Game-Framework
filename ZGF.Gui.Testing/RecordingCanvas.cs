using System.Numerics;
using ZGF.Geometry;

namespace ZGF.Gui.Testing;

public abstract record DrawCommand(int Sequence, RectF? Clip, float Opacity, float TranslationX, float TranslationY);

public sealed record RecordedRect(DrawRectInputs Inputs, int Sequence, RectF? Clip, float Opacity, float TranslationX, float TranslationY) : DrawCommand(Sequence, Clip, Opacity, TranslationX, TranslationY);
public sealed record RecordedText(DrawTextInputs Inputs, int Sequence, RectF? Clip, float Opacity, float TranslationX, float TranslationY) : DrawCommand(Sequence, Clip, Opacity, TranslationX, TranslationY);
public sealed record RecordedImage(DrawImageInputs Inputs, int Sequence, RectF? Clip, float Opacity, float TranslationX, float TranslationY) : DrawCommand(Sequence, Clip, Opacity, TranslationX, TranslationY);
public sealed record RecordedBoxShadow(DrawBoxShadowInputs Inputs, int Sequence, RectF? Clip, float Opacity, float TranslationX, float TranslationY) : DrawCommand(Sequence, Clip, Opacity, TranslationX, TranslationY);
public sealed record RecordedLine(DrawLineInputs Inputs, int Sequence, RectF? Clip, float Opacity, float TranslationX, float TranslationY) : DrawCommand(Sequence, Clip, Opacity, TranslationX, TranslationY);
public sealed record RecordedCircle(DrawCircleInputs Inputs, int Sequence, RectF? Clip, float Opacity, float TranslationX, float TranslationY) : DrawCommand(Sequence, Clip, Opacity, TranslationX, TranslationY);
public sealed record RecordedBezier(DrawBezierInputs Inputs, int Sequence, RectF? Clip, float Opacity, float TranslationX, float TranslationY) : DrawCommand(Sequence, Clip, Opacity, TranslationX, TranslationY);

/// <summary>Captures every draw call into typed lists with the clip in effect and a draw-order
/// sequence index, so tests can assert what was drawn instead of pixels. Text metrics come from a
/// pluggable <see cref="ITextMeasurer"/>; image sizes default to zero unless registered.</summary>
public sealed class RecordingCanvas : ICanvas
{
    private readonly ITextMeasurer _measurer;
    private readonly Stack<RectF> _clips = new();
    private readonly Stack<float> _opacities = new();
    private readonly Stack<Vector2> _translations = new();
    private readonly Dictionary<string, (int Width, int Height)> _imageSizes = new();
    private int _sequence;

    private readonly List<RecordedRect> _rects = new();
    private readonly List<RecordedText> _texts = new();
    private readonly List<RecordedImage> _images = new();
    private readonly List<RecordedBoxShadow> _boxShadows = new();
    private readonly List<RecordedLine> _lines = new();
    private readonly List<RecordedCircle> _circles = new();
    private readonly List<RecordedBezier> _beziers = new();
    private readonly List<DrawCommand> _all = new();

    public IReadOnlyList<RecordedRect> Rects => _rects;
    public IReadOnlyList<RecordedText> Texts => _texts;
    public IReadOnlyList<RecordedImage> Images => _images;
    public IReadOnlyList<RecordedBoxShadow> BoxShadows => _boxShadows;
    public IReadOnlyList<RecordedLine> Lines => _lines;
    public IReadOnlyList<RecordedCircle> Circles => _circles;
    public IReadOnlyList<RecordedBezier> Beziers => _beziers;

    public int DefaultImageWidth { get; set; }
    public int DefaultImageHeight { get; set; }

    public RecordingCanvas(ITextMeasurer? measurer = null)
    {
        _measurer = measurer ?? new SyntheticTextMeasurer();
    }

    public void SetImageSize(string imageId, int width, int height) => _imageSizes[imageId] = (width, height);

    private RectF? CurrentClip() => _clips.Count > 0 ? _clips.Peek() : null;
    private float CurrentOpacity() => _opacities.Count > 0 ? _opacities.Peek() : 1f;
    private Vector2 CurrentTranslation() => _translations.Count > 0 ? _translations.Peek() : Vector2.Zero;

    public void DrawRect(in DrawRectInputs inputs)
    {
        var t = CurrentTranslation();
        var cmd = new RecordedRect(inputs, _sequence++, CurrentClip(), CurrentOpacity(), t.X, t.Y);
        _rects.Add(cmd);
        _all.Add(cmd);
    }

    public void DrawText(in DrawTextInputs inputs)
    {
        var t = CurrentTranslation();
        var cmd = new RecordedText(inputs, _sequence++, CurrentClip(), CurrentOpacity(), t.X, t.Y);
        _texts.Add(cmd);
        _all.Add(cmd);
    }

    public void DrawImage(in DrawImageInputs inputs)
    {
        var t = CurrentTranslation();
        var cmd = new RecordedImage(inputs, _sequence++, CurrentClip(), CurrentOpacity(), t.X, t.Y);
        _images.Add(cmd);
        _all.Add(cmd);
    }

    public void DrawBoxShadow(in DrawBoxShadowInputs inputs)
    {
        var t = CurrentTranslation();
        var cmd = new RecordedBoxShadow(inputs, _sequence++, CurrentClip(), CurrentOpacity(), t.X, t.Y);
        _boxShadows.Add(cmd);
        _all.Add(cmd);
    }

    public void DrawLine(in DrawLineInputs inputs)
    {
        var t = CurrentTranslation();
        var cmd = new RecordedLine(inputs, _sequence++, CurrentClip(), CurrentOpacity(), t.X, t.Y);
        _lines.Add(cmd);
        _all.Add(cmd);
    }

    public void DrawCircle(in DrawCircleInputs inputs)
    {
        var t = CurrentTranslation();
        var cmd = new RecordedCircle(inputs, _sequence++, CurrentClip(), CurrentOpacity(), t.X, t.Y);
        _circles.Add(cmd);
        _all.Add(cmd);
    }

    public void DrawBezier(in DrawBezierInputs inputs)
    {
        var t = CurrentTranslation();
        var cmd = new RecordedBezier(inputs, _sequence++, CurrentClip(), CurrentOpacity(), t.X, t.Y);
        _beziers.Add(cmd);
        _all.Add(cmd);
    }

    public bool TryGetClip(out RectF rect)
    {
        if (_clips.Count > 0)
        {
            rect = _clips.Peek();
            return true;
        }
        rect = default;
        return false;
    }

    public void PushClip(RectF rect) => _clips.Push(rect);

    public void PopClip()
    {
        if (_clips.Count > 0)
            _clips.Pop();
    }

    public void PushOpacity(float opacity) => _opacities.Push(CurrentOpacity() * Math.Clamp(opacity, 0f, 1f));
    public void PopOpacity() { if (_opacities.Count > 0) _opacities.Pop(); }
    public void PushTranslation(float dx, float dy) => _translations.Push(CurrentTranslation() + new Vector2(dx, dy));
    public void PopTranslation() { if (_translations.Count > 0) _translations.Pop(); }

    public float MeasureTextWidth(ReadOnlySpan<char> text, TextStyle style) =>
        _measurer.MeasureTextWidth(text, style);

    public float MeasureTextPrefix(ReadOnlySpan<char> text, int prefixLength, TextStyle style) =>
        _measurer.MeasureTextPrefix(text, prefixLength, style);

    public float MeasureTextLineHeight(TextStyle style) => _measurer.MeasureTextLineHeight(style);

    public int GetImageWidth(string imageId) =>
        _imageSizes.TryGetValue(imageId, out var size) ? size.Width : DefaultImageWidth;

    public int GetImageHeight(string imageId) =>
        _imageSizes.TryGetValue(imageId, out var size) ? size.Height : DefaultImageHeight;

    /// <summary>Every captured command merged in draw order.</summary>
    public IReadOnlyList<DrawCommand> InDrawOrder() => _all;

    /// <summary>Clears all captured commands, the clip stack, and the sequence; called per render.</summary>
    public void Reset()
    {
        _rects.Clear();
        _texts.Clear();
        _images.Clear();
        _boxShadows.Clear();
        _lines.Clear();
        _circles.Clear();
        _beziers.Clear();
        _all.Clear();
        _clips.Clear();
        _opacities.Clear();
        _translations.Clear();
        _sequence = 0;
    }
}
