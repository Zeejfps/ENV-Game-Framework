using ZGF.Geometry;

namespace ZGF.Gui;

public interface ICanvas
{
    void DrawRect(in DrawRectInputs inputs);
    void DrawText(in DrawTextInputs inputs);
    void DrawImage(in DrawImageInputs inputs);
    void DrawBoxShadow(in DrawBoxShadowInputs inputs);
    void DrawLine(in DrawLineInputs inputs);
    void DrawCircle(in DrawCircleInputs inputs);
    void DrawBezier(in DrawBezierInputs inputs);
    
    bool TryGetClip(out RectF rect);
    void PushClip(RectF rect);
    void PopClip();
    
    float MeasureTextWidth(ReadOnlySpan<char> text, TextStyle style);

    /// <summary>
    /// Visual width (logical points) of the first <paramref name="prefixLength"/> UTF-16 units of a
    /// single line of <paramref name="text"/>, shaped <b>in context of the whole line</b> — the
    /// in-context analogue of <c>MeasureTextWidth(text[..prefixLength])</c>. Use it for caret/selection
    /// positioning: in cursive scripts (Arabic) a letter's advance depends on its neighbors, and a
    /// combining mark has zero advance, so re-measuring a detached prefix gives a wrong x. Computed by
    /// summing the advances of the shaped glyphs whose logical cluster precedes the caret, so it is
    /// exact for a unidirectional line (approximate across a bidi boundary).
    /// </summary>
    float MeasureTextPrefix(ReadOnlySpan<char> text, int prefixLength, TextStyle style);

    float MeasureTextLineHeight(TextStyle style);

    int GetImageWidth(string imageId);
    int GetImageHeight(string imageId);
}