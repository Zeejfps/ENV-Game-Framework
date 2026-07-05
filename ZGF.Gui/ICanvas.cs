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
    void DrawCubicBezier(in DrawCubicBezierInputs inputs);

    bool TryGetClip(out RectF rect);
    void PushClip(RectF rect);
    void PopClip();

    /// <summary>Multiplies a render-only opacity onto everything drawn until the matching
    /// <see cref="PopOpacity"/>; composes (nests) with any opacity already on the stack.</summary>
    void PushOpacity(float opacity);
    void PopOpacity();

    /// <summary>Offsets everything drawn (and nested clips) by (dx, dy) logical points until the
    /// matching <see cref="PopTranslation"/>; composes (nests) with any active transform. Affects
    /// drawing only — never layout.</summary>
    void PushTranslation(float dx, float dy);
    void PopTranslation();

    /// <summary>Scales everything drawn (and nested clips) by (sx, sy) about the pivot point
    /// (pivotX, pivotY) — given in the current local coordinate space — until the matching
    /// <see cref="PopScale"/>; composes (nests) with any active transform. Affects drawing only —
    /// never layout. Pass a view's center as the pivot for a pop/zoom animation.</summary>
    void PushScale(float sx, float sy, float pivotX, float pivotY);
    void PopScale();

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

    /// <summary>Device pixels per logical point (1 on non-HiDPI surfaces).</summary>
    float DpiScale => 1f;

    /// <summary>
    /// Creates the image under <paramref name="imageId"/>, or replaces its pixels
    /// (and size) if it already exists. Pixel data is straight-alpha RGBA8 in
    /// top-down row order. Returns false on canvases without image support.
    /// </summary>
    bool CreateOrUpdateRgbaImage(string imageId, int widthPx, int heightPx, ReadOnlySpan<byte> rgbaTopDown) => false;

    bool HasImage(string imageId) => false;

    void RemoveImage(string imageId) { }
}