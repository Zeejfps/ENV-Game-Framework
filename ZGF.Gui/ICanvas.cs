using ZGF.Geometry;

namespace ZGF.Gui;

public interface ICanvas
{
    void DrawRect(in DrawRectInputs inputs);
    void DrawText(in DrawTextInputs inputs);

    /// <summary>
    /// Draws consecutive monospaced cells at a fixed pitch, one code point per cell.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The cell-grid counterpart to <see cref="DrawText"/>. Each code point goes straight to a
    /// glyph and onto its own column, with no shaping, no ligatures and no kerning — which is what
    /// makes it right for a terminal, where the grid decides the columns and the font does not get
    /// a say, and wrong for prose, where a glyph's form and advance depend on its neighbours.
    /// </para>
    /// <para>
    /// A cell two columns wide (CJK, most emoji) is drawn by putting its code point on the leading
    /// column and a space on the trailing one, so the glyph overhangs into the column the grid has
    /// already reserved for it. A cell holding a whole grapheme cluster rather than a single code
    /// point cannot be expressed here and belongs in <see cref="DrawText"/>.
    /// </para>
    /// </remarks>
    void DrawGlyphRun(in DrawGlyphRunInputs inputs);
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

    /// <summary>
    /// The box one cell occupies for <paramref name="style"/>, and the pitch
    /// <see cref="DrawGlyphRun"/> will place columns at. See <see cref="CellMetrics"/>.
    /// </summary>
    CellMetrics MeasureCellSize(TextStyle style);

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