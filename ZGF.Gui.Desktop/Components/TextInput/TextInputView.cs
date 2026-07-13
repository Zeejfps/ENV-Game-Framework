using ZGF.Desktop.Input;
using ZGF.Fonts;
using ZGF.Geometry;
using ZGF.Observable;

namespace ZGF.Gui.Desktop.Components.TextInput;

public sealed class TextInputView : View
{
    private StyleValue<TextWrap> _textWrap;
    public StyleValue<TextWrap> TextWrap
    {
        get => _textWrap;
        set
        {
            if (SetField(ref _textWrap, value))
                InvalidateLines();
        }
    }

    public uint BackgroundColor
    {
        get => _background.BackgroundColor;
        set => SetField(ref _background.BackgroundColor, value);
    }

    public StyleValue<uint> TextColor
    {
        get => _textStyle.TextColor;
        set => SetField(ref _textStyle.TextColor, value);
    }

    public StyleValue<float> FontSize
    {
        get => _textStyle.FontSize;
        set
        {
            if (SetField(ref _textStyle.FontSize, value))
                InvalidateLines();
        }
    }

    public StyleValue<FontFeatureSet> FontFeatures
    {
        get => _textStyle.FontFeatures;
        set
        {
            if (SetField(ref _textStyle.FontFeatures, value))
                InvalidateLines();
        }
    }

    public StyleValue<TextAlignment> TextVerticalAlignment
    {
        get => _textStyle.VerticalAlignment;
        set => SetField(ref _textStyle.VerticalAlignment, value);
    }

    public uint SelectionRectColor
    {
        get => _selectionRectStyle.BackgroundColor;
        set => SetField(ref _selectionRectStyle.BackgroundColor, value);
    }

    public uint CaretColor
    {
        get => _cursorStyle.BackgroundColor;
        set => SetField(ref _cursorStyle.BackgroundColor, value);
    }

    private string? _placeholderText;
    public string? PlaceholderText
    {
        get => _placeholderText;
        set => SetField(ref _placeholderText, value);
    }

    private StyleValue<uint> _placeholderColor;
    public StyleValue<uint> PlaceholderTextColor
    {
        get => _placeholderColor;
        set => SetField(ref _placeholderColor, value);
    }

    public bool IsSelecting => _caretIndex != _selectionStartIndex;

    /// <summary>The field's resolved writing direction (from content first-strong, last draw). Arrow-key
    /// handling reads it so Left/Right move the caret visually rather than logically under RTL.</summary>
    public bool IsContentRtl => _contentRtl;

    private readonly RectStyle _background = new();
    private readonly TextStyle _textStyle = new();
    private readonly RectStyle _cursorStyle = new();
    private readonly RectStyle _selectionRectStyle = new();
    // Takes the resolved text color at draw time, so a composition always contrasts with the text
    // it sits among without the theme having to say anything about it.
    private readonly RectStyle _preeditUnderlineStyle = new();

    private const float CaretWidth = 2f;
    private const float UnderlineThickness = 1f;
    private const float FocusedUnderlineThickness = 2f;

    private int _caretIndex;
    private int _strLen;
    private bool _isEditing;
    private int _selectionStartIndex;
    private float _scrollOffsetX;

    // The field's resolved writing direction, recomputed each draw from the content's first strong
    // character (an empty field follows the UI direction). Drives text alignment + the caret/selection
    // origin so an LTR identifier reads left-aligned and Arabic input reads right-aligned, regardless
    // of locale. Per-character caret precision holds for unidirectional lines; mixed bidi within one
    // line is approximate (a true fix needs per-cluster shaper mapping).
    private bool _contentRtl;

    // Sticky x for vertical movement: the pixel column Up/Down aim for. Set on the first
    // Up/Down of a run and reused so passing through a short line doesn't shrink the column;
    // any horizontal move or edit clears it (-1) so the next Up/Down re-anchors to the caret.
    private float _goalColumnX = -1f;
    private char[] _buffer;
    private readonly State<string> _text = new(string.Empty);

    // The in-flight IME composition, held apart from _buffer so it never becomes the field's value:
    // half-typed pinyin must not reach whatever is bound to TextValue. _composed is _buffer with the
    // composition spliced in at the caret — what the user sees, and the only thing drawing and
    // measuring read, so the preedit renders inline without ever being typed.
    private PreeditText _preedit = PreeditText.Empty;
    private char[] _composed = new char[64];
    private int _composedLen;

    private readonly List<Range> _lines = new();
    private float _linesWidth = -1f;
    private int _linesVersion = -1;
    private int _version;

    /// <summary>Zero-allocation view of the current text. Prefer this for renderers,
    /// controllers, and equality checks; use <see cref="TextValue"/> to observe changes.</summary>
    public ReadOnlySpan<char> Text => _buffer.AsSpan(0, _strLen);

    /// <summary>True while an IME composition is in flight. The field's value is unchanged during
    /// one — nothing has been typed yet — but keys belong to the IME, not to editing.</summary>
    public bool IsComposing => !_preedit.IsEmpty;

    // Text as drawn: the buffer with any composition spliced in at the caret. Equal to Text when
    // nothing is composing, which is why every draw/measure path can read it unconditionally.
    private ReadOnlySpan<char> DisplayText => IsComposing ? _composed.AsSpan(0, _composedLen) : Text;
    private int DisplayLength => IsComposing ? _composedLen : _strLen;
    // Within a composition the caret is the IME's, which sits inside the preedit rather than at its end.
    private int DisplayCaret => IsComposing ? _caretIndex + _preedit.Caret : _caretIndex;

    /// <summary>
    /// Replaces the in-flight composition. An empty <paramref name="preedit"/> ends it, which means
    /// only "stop showing this" — it says nothing about whether the user committed or cancelled, and
    /// committed text arrives separately through <see cref="Enter(System.ReadOnlySpan{char})"/>.
    /// </summary>
    public void SetComposition(PreeditText preedit)
    {
        // A composition replaces the selection, the same as typing would. That deletion is a real
        // edit to the buffer, so unlike the composition itself it does notify — otherwise cancelling
        // the composition would leave the field rendering empty while TextValue still held the
        // replaced text.
        if (!preedit.IsEmpty && !IsComposing && IsSelecting)
        {
            DeleteSelection();
            SyncText();
        }

        _preedit = preedit;
        RebuildComposed();
        InvalidateLines();
        SetDirty();
    }

    /// <summary>Drops the composition without committing it. The IME still holds one of its own —
    /// callers that want it gone for good must also reset the IME.</summary>
    public void ClearComposition()
    {
        if (!IsComposing)
            return;

        _preedit = PreeditText.Empty;
        _composedLen = 0;
        InvalidateLines();
        SetDirty();
    }

    private void RebuildComposed()
    {
        if (!IsComposing)
        {
            _composedLen = 0;
            return;
        }

        var preedit = _preedit.Text;
        var required = _strLen + preedit.Length;
        if (_composed.Length < required)
            Array.Resize(ref _composed, Math.Max(required, _composed.Length * 2));

        var composed = _composed.AsSpan();
        _buffer.AsSpan(0, _caretIndex).CopyTo(composed);
        preedit.AsSpan().CopyTo(composed[_caretIndex..]);
        _buffer.AsSpan(_caretIndex, _strLen - _caretIndex).CopyTo(composed[(_caretIndex + preedit.Length)..]);
        _composedLen = required;
    }

    /// <summary>
    /// The current text as an observable value, updated on every buffer mutation (a
    /// programmatic <see cref="SetText"/> emits a single notification). Read-only to callers:
    /// drive edits through the keyboard controller, <see cref="Enter(System.ReadOnlySpan{char})"/>,
    /// or <see cref="SetText"/>. Replaces the former <c>TextChanged</c> event so input text can
    /// participate in the observable graph (bindings, <c>Derived</c>) like any other state.
    /// </summary>
    public IReadable<string> TextValue => _text;
    public bool IsEditing => _isEditing;

    private readonly ICanvas _canvas;

    public TextInputView(ICanvas canvas)
    {
        _canvas = canvas;
        _buffer = new char[512];
        _cursorStyle.BackgroundColor = 0xFF000000;
        _selectionRectStyle.BackgroundColor = 0xFF8aadff;
    }

    public void StartEditing()
    {
        _isEditing = true;
    }
    
    public void StopEditing()
    {
        _isEditing = false;
    }

    private int GetCaretIndexFromPoint(in PointF point)
    {
        if (_strLen == 0)
            return 0;

        if (point.Y >= Position.Top)
        {
            return 0;
        }

        if (point.Y <= Position.Bottom)
        {
            return _strLen;
        }

        // xOffset is the click's distance from the line's leading edge — the left under LTR, the
        // right under RTL — matched below against logical prefix widths (also leading-relative).
        var xOffset = _contentRtl
            ? Position.Right + _scrollOffsetX - point.X
            : point.X - Position.Left + _scrollOffsetX;
        var canvas = _canvas;
        var lineCount = 1;
        var lineHeight = canvas.MeasureTextLineHeight(_textStyle);

        var lines = GetLines(Position.Width, canvas);
        foreach (var line in lines)
        {
            var lineBottom = Position.Top - lineCount * lineHeight;
            var lineTop = lineBottom + lineHeight;

            if (point.Y < lineTop && point.Y >= lineBottom)
            {
                var lineStartIndex = line.Start.Value;
                var lineEndIndex = line.End.Value;

                // Walk the line a cluster at a time: the boundary x between two clusters is their
                // midpoint, so clicking past it lands the caret after the cluster — never inside it.
                var text = Text;
                var prevIndex = lineStartIndex;
                var prevOffset = LineOffsetOf(lineStartIndex, lineStartIndex, canvas);
                var i = lineStartIndex;
                while (i < lineEndIndex)
                {
                    var next = TextBoundaries.Next(text, i);
                    if (next <= i)
                        break;

                    var offset = LineOffsetOf(next, lineStartIndex, canvas);
                    if ((offset + prevOffset) * 0.5f > xOffset)
                        return prevIndex;

                    prevIndex = next;
                    prevOffset = offset;
                    i = next;
                }

                return prevIndex;
            }

            lineCount++;
        }

        return _strLen;
    }

    private void DeleteSelection()
    {
        var min = _selectionStartIndex;
        var max = _caretIndex;
        if (min > max)
        {
            min = _caretIndex;
            max = _selectionStartIndex;
        }
        DeleteRange(min, max);
        _caretIndex = min;
        _selectionStartIndex = _caretIndex;
    }

    private void DeleteRange(int min, int max)
    {
        var delta = max - min;
        for (var i = max; i < _strLen; i++)
        {
            _buffer[i - delta] = _buffer[i];
        }
        _strLen -= delta;
        if (_caretIndex > _strLen)
        {
            _caretIndex = _strLen;
        }
    }

    private void EnsureCapacity(int required)
    {
        if (required <= _buffer.Length)
            return;

        var newCapacity = _buffer.Length * 2;
        while (newCapacity < required)
            newCapacity *= 2;
        Array.Resize(ref _buffer, newCapacity);
    }

    private void InsertChar(int index, char c)
    {
        EnsureCapacity(_strLen + 1);
        _strLen++;
        for (var i = _strLen - 1; i > index; i--)
        {
            _buffer[i] = _buffer[i-1];
        }
        _buffer[index] = c;
    }

    protected override float MeasureHeightIntrinsic(float availableWidth)
    {
        var canvas = _canvas;

        var lineHeight = canvas.MeasureTextLineHeight(_textStyle);
        if (DisplayLength == 0)
            return lineHeight;

        // availableWidth <= 0 means "unconstrained" — fall back to intrinsic width so we
        // still report a sensible (single-line) height instead of one line per character.
        var width = availableWidth > 0f ? availableWidth : MeasureWidth();
        var height = GetLines(width, canvas).Count * lineHeight;
        if (Height.IsSet && height < Height)
            return Height;

        return height;
    }

    // The field's visual lines, as ranges into the buffer: soft wraps from TextWrapper (word
    // boundaries, kinsoku, never inside a surrogate pair) plus the hard '\n' breaks. Everything that
    // needs line geometry — drawing, the caret rect, selection rects, clicks, Up/Down — reads these,
    // so they cannot disagree about where a line ends. Cached until the text, the width or a
    // metrics-affecting style changes.
    private IReadOnlyList<Range> GetLines(float width, ICanvas canvas)
    {
        if (_linesVersion == _version && _linesWidth == width)
            return _lines;

        _lines.Clear();
        if (TextWrap == Gui.TextWrap.Wrap)
            TextWrapper.WrapRanges(canvas, DisplayText, _textStyle, width, _lines);
        else
            _lines.Add(new Range(0, DisplayLength));

        _linesVersion = _version;
        _linesWidth = width;
        return _lines;
    }

    private void UpdateScrollOffset(in RectF position, ICanvas c)
    {
        if (TextWrap == Gui.TextWrap.Wrap)
        {
            _scrollOffsetX = 0f;
            return;
        }

        var caretX = c.MeasureTextPrefix(DisplayText, DisplayCaret, _textStyle);
        var width = position.Width;

        if (caretX - _scrollOffsetX < 0f)
        {
            _scrollOffsetX = caretX;
        }
        else if (caretX - _scrollOffsetX > width - CaretWidth)
        {
            _scrollOffsetX = caretX - width + CaretWidth;
        }

        var totalWidth = c.MeasureTextWidth(DisplayText, _textStyle);
        var maxOffset = Math.Max(0f, totalWidth - width + CaretWidth);
        _scrollOffsetX = Math.Clamp(_scrollOffsetX, 0f, maxOffset);
    }

    // The field is RTL when its first strong character is RTL; an empty field follows the UI
    // direction so the caret rests on the correct side before anything is typed.
    private bool ResolveContentRtl()
    {
        var text = DisplayText;
        if (text.IsEmpty)
            return IsRtl;
        Bidi.ResolveLevels(text, BidiDirection.Auto, out var paragraphLevel);
        return paragraphLevel == 1;
    }

    protected override void OnDrawSelf(ICanvas c)
    {
        var position = Position;

        // Resolve direction first so text alignment (via the style's base direction) and the mirrored
        // caret/selection origin below agree within this draw.
        _contentRtl = ResolveContentRtl();
        _textStyle.BaseDirection = _contentRtl ? BidiDirection.Rtl : BidiDirection.Ltr;

        DrawBackground(position, c);

        UpdateScrollOffset(position, c);

        c.PushClip(position);

        // One vertical offset, shared by text/placeholder/caret/selection so they stay aligned.
        var lineHeight = c.MeasureTextLineHeight(_textStyle);
        var verticalOffset = VerticalTextOffset(position, lineHeight, c);

        // A composition collapses the selection, so there is never both.
        if (_isEditing && !IsComposing && _selectionStartIndex != _caretIndex)
        {
            DrawSelectionBox(position, c, verticalOffset);
        }

        if (DisplayLength == 0)
        {
            DrawPlaceholder(position, c, verticalOffset);
        }
        else
        {
            DrawText(position, c, verticalOffset);
        }

        if (IsComposing)
        {
            DrawPreeditUnderlines(position, c, verticalOffset);
        }

        if (_isEditing)
        {
            DrawCaret(position, c, verticalOffset);
        }

        c.PopClip();
    }

    // The clause underlines that mark text as composed-but-not-typed. The IME splits the composition
    // into clauses it converts independently; the focused one — the clause the user is working on —
    // is underlined heavily, the rest lightly. An IME that reports no clauses gets one underline
    // spanning the whole composition.
    private void DrawPreeditUnderlines(in RectF position, ICanvas c, float verticalOffset)
    {
        _preeditUnderlineStyle.BackgroundColor =
            _textStyle.TextColor.IsSet ? _textStyle.TextColor.Value : 0xFF000000;

        var blocks = _preedit.Blocks;
        if (blocks.Count == 0)
        {
            DrawRangeRects(_caretIndex, _caretIndex + _preedit.Text.Length, position, c, verticalOffset,
                _preeditUnderlineStyle, UnderlineThickness);
            return;
        }

        for (var i = 0; i < blocks.Count; i++)
        {
            var start = _caretIndex + blocks[i].Start;
            var thickness = i == _preedit.FocusedBlock ? FocusedUnderlineThickness : UnderlineThickness;
            DrawRangeRects(start, start + blocks[i].Length, position, c, verticalOffset,
                _preeditUnderlineStyle, thickness);
        }
    }

    private void DrawPlaceholder(in RectF position, ICanvas c, float verticalOffset)
    {
        if (string.IsNullOrEmpty(_placeholderText))
            return;

        // Swap the text color for the placeholder color while reusing the rest of
        // _textStyle (font, alignment, wrap). Restored before returning so subsequent
        // draws — once the user types — pick up the original color again.
        var originalColor = _textStyle.TextColor;
        if (_placeholderColor.IsSet)
            _textStyle.TextColor = _placeholderColor;

        var lineHeight = c.MeasureTextLineHeight(_textStyle);
        c.DrawText(new DrawTextInputs
        {
            Position = new RectF
            {
                Left = position.Left,
                Bottom = position.Top - lineHeight + verticalOffset,
                Width = position.Width,
                Height = lineHeight,
            },
            Text = _placeholderText!,
            Style = _textStyle,
            ZIndex = GetDrawZIndex(),
        });

        _textStyle.TextColor = originalColor;
    }

    private void DrawBackground(in RectF position, ICanvas c)
    {
        c.DrawRect(new DrawRectInputs
        {
            Position = position,
            Style = _background,
            ZIndex = GetDrawZIndex()
        });
    }

    private void DrawSelectionBox(in RectF position, ICanvas c, float verticalOffset)
    {
        var min = Math.Min(_selectionStartIndex, _caretIndex);
        var max = Math.Max(_selectionStartIndex, _caretIndex);
        var lineHeight = c.MeasureTextLineHeight(_textStyle);
        DrawRangeRects(min, max, position, c, verticalOffset, _selectionRectStyle, lineHeight);
    }

    // One rect per visual line the range [min,max) touches, clipped to that line. Both the selection
    // highlight and the composition underlines need this, because either can straddle a soft wrap;
    // they differ only in height — a full line box versus a hairline sitting on the baseline.
    private void DrawRangeRects(int min, int max, in RectF position, ICanvas c, float verticalOffset,
        RectStyle style, float height)
    {
        var lineHeight = c.MeasureTextLineHeight(_textStyle);
        var lines = GetLines(position.Width, c);

        for (var i = 0; i < lines.Count; i++)
        {
            var lineStart = lines[i].Start.Value;
            var lineEnd = lines[i].End.Value;
            if (max < lineStart || min > lineEnd)
                continue;

            var segStart = Math.Max(min, lineStart);
            var segEnd = Math.Min(max, lineEnd);

            var startOffset = LineOffsetOf(segStart, lineStart, c);
            var width = LineOffsetOf(segEnd, lineStart, c) - startOffset;
            var left = position.Left + startOffset - _scrollOffsetX;
            var rect = new RectF
            {
                // Mirror the segment within the field for RTL, so it lands on the right-aligned text.
                Left = _contentRtl ? position.Left + position.Right - left - width : left,
                Bottom = position.Top - (i + 1) * lineHeight + verticalOffset,
                Width = width,
                Height = height,
            };

            c.DrawRect(new DrawRectInputs
            {
                Position = rect,
                Style = style,
                ZIndex = GetDrawZIndex()
            });
        }
    }

    // The x of an index within its visual line. A single line measures the prefix in context
    // (cursive-correct, and a zero-width mark doesn't shift it); a wrapped line measures its segment.
    private float LineOffsetOf(int index, int lineStart, ICanvas c) =>
        TextWrap != Gui.TextWrap.Wrap
            ? c.MeasureTextPrefix(DisplayText, index, _textStyle)
            : c.MeasureTextWidth(DisplayText.Slice(lineStart, index - lineStart), _textStyle);

    private void DrawText(in RectF position, ICanvas c, float verticalOffset)
    {
        var lineHeight = c.MeasureTextLineHeight(_textStyle);
        // Under RTL the line box shifts the other way so the canvas (which right-aligns Start text on
        // an RTL base) ends each line at the right edge; scrolling reveals the trailing/left content.
        var left = _contentRtl ? position.Left + _scrollOffsetX : position.Left - _scrollOffsetX;
        var bottom = position.Top - lineHeight + verticalOffset;
        var lines = GetLines(position.Width, c);
        foreach (var line in lines)
        {
            c.DrawText(new DrawTextInputs
            {
                Position = new RectF
                {
                    Left = left,
                    Bottom = bottom,
                    Width = position.Width,
                    Height = lineHeight,
                },
                Text = DisplayText[line].ToString(),
                Style = _textStyle,
                ZIndex = GetDrawZIndex()
            });
            bottom -= lineHeight;
        }
    }

    // Shifts the whole text block down to vertically center it in the box, returning a non-positive
    // offset added to the top-aligned baseline. Centering is the default for single-line (NoWrap)
    // inputs — where top-pinning makes descenders graze (and the box clip cut) the bottom edge — and
    // off for multi-line (Wrap) editors, where the first line must stay put as the user types. An
    // explicit VerticalAlignment overrides that default either way. Stays 0 when the text is as tall
    // as / taller than the box, so the first line is never pushed off the top. Computed once per
    // draw in OnDrawSelf and shared by text/placeholder/caret/selection so they stay aligned.
    private float VerticalTextOffset(in RectF position, float lineHeight, ICanvas c)
    {
        var center = _textStyle.VerticalAlignment.IsSet
            ? _textStyle.VerticalAlignment.Value == TextAlignment.Center
            : TextWrap != Gui.TextWrap.Wrap;
        if (!center)
            return 0f;

        var lineCount = DisplayLength == 0 ? 1 : GetLines(position.Width, c).Count;
        var slack = position.Height - lineCount * lineHeight;
        return slack > 0f ? -slack * 0.5f : 0f;
    }

    /// <summary>
    /// The caret's current rect in absolute (canvas) coordinates — the same rect the caret is
    /// painted at. Lets a scroll container (<see cref="IScrollScope"/>) keep the caret in view.
    /// </summary>
    public RectF GetCaretRect()
    {
        var position = Position;
        var lineHeight = _canvas.MeasureTextLineHeight(_textStyle);
        return ComputeCaretRect(position, _canvas, VerticalTextOffset(position, lineHeight, _canvas));
    }

    private RectF ComputeCaretRect(in RectF position, ICanvas canvas, float verticalOffset)
    {
        // The composition's caret, when there is one: it sits inside the preedit, and the OS
        // candidate window is positioned against this rect.
        var caret = DisplayCaret;
        var lines = GetLines(position.Width, canvas);
        var lineIndex = FindLineIndex(lines, caret);
        var startIndex = lines[lineIndex].Start.Value;

        var lineHeight = canvas.MeasureTextLineHeight(_textStyle);
        var cursorHeight = lineHeight;
        // Single line: measure the caret prefix in the context of the whole line, so a cursive
        // Arabic caret lands correctly and a zero-width mark doesn't shift it. (Wrapped lines keep
        // the per-segment measure.)
        var cursorPosLeft = TextWrap != Gui.TextWrap.Wrap
            ? canvas.MeasureTextPrefix(DisplayText, caret, _textStyle)
            : canvas.MeasureTextWidth(DisplayText.Slice(startIndex, caret - startIndex), _textStyle);
        var cursorPosBottom = position.Top - (lineIndex + 1) * lineHeight + verticalOffset;

        return new RectF
        {
            Bottom = cursorPosBottom,
            // Logical prefix width measures from the leading edge: left edge under LTR, right under RTL.
            Left = _contentRtl
                ? position.Right + _scrollOffsetX - cursorPosLeft - CaretWidth
                : position.Left + cursorPosLeft - _scrollOffsetX,
            Width = CaretWidth,
            Height = cursorHeight
        };
    }

    private void DrawCaret(in RectF position, ICanvas canvas, float verticalOffset)
    {
        canvas.DrawRect(new DrawRectInputs
        {
            Position = ComputeCaretRect(position, canvas, verticalOffset),
            Style = _cursorStyle,
            ZIndex = GetDrawZIndex()
        });
    }
    
    public void MoveCaretTo(PointF point, bool isSelecting = false)
    {
        _goalColumnX = -1f;
        SetCaret(GetCaretIndexFromPoint(point), isSelecting);
    }

    public void SelectAll()
    {
        _goalColumnX = -1f;
        _selectionStartIndex = 0;
        _caretIndex = _strLen;
    }

    public void MoveCaretLeft(bool select = false)
    {
        _goalColumnX = -1f;
        var isSelecting = IsSelecting;
        if (!isSelecting || select)
        {
            _caretIndex = TextBoundaries.Prev(Text, _caretIndex);
        }
                
        if (!select)
        {
            if (_selectionStartIndex < _caretIndex)
            {
                _caretIndex = _selectionStartIndex;
            }
            else if (_selectionStartIndex > _caretIndex)
            {
                _selectionStartIndex = _caretIndex;
            }
        }
    }

    public void MoveCaretRight(bool select = false)
    {
        _goalColumnX = -1f;
        var isSelecting = IsSelecting;
        if (!isSelecting || select)
        {
            _caretIndex = TextBoundaries.Next(Text, _caretIndex);
        }
                
        if (!select)
        {
            if (_selectionStartIndex < _caretIndex)
            {
                _selectionStartIndex = _caretIndex;
            }
            else if (_selectionStartIndex > _caretIndex)
            {
                _caretIndex = _selectionStartIndex;
            }
        }
    }

    public void MoveCaretLeftWord(bool select = false)
    {
        _goalColumnX = -1f;
        _caretIndex = FindPreviousWordBoundary(_caretIndex);
        if (!select)
            _selectionStartIndex = _caretIndex;
    }

    public void MoveCaretRightWord(bool select = false)
    {
        _goalColumnX = -1f;
        _caretIndex = FindNextWordBoundary(_caretIndex);
        if (!select)
            _selectionStartIndex = _caretIndex;
    }

    private int FindPreviousWordBoundary(int index) => TextBoundaries.PrevWord(Text, index);

    private int FindNextWordBoundary(int index) => TextBoundaries.NextWord(Text, index);

    public void MoveCaretDown(bool select = false) => MoveCaretVertically(1, select);

    public void MoveCaretUp(bool select = false) => MoveCaretVertically(-1, select);

    // Vertical movement walks the same visual lines GetLines/DrawText produce (hard '\n'
    // breaks plus soft wraps), and tracks the caret's pixel x so the column is preserved
    // across proportional fonts rather than by character count.
    private void MoveCaretVertically(int direction, bool select)
    {
        var canvas = _canvas;

        var lines = GetLines(Position.Width, canvas);
        var lineIndex = FindLineIndex(lines, _caretIndex);

        var lineStart = lines[lineIndex].Start.Value;
        if (_goalColumnX < 0f)
            _goalColumnX = canvas.MeasureTextWidth(_buffer.AsSpan(lineStart, _caretIndex - lineStart), _textStyle);

        var targetLineIndex = lineIndex + direction;
        if (targetLineIndex < 0)
        {
            SetCaret(0, select);
            return;
        }
        if (targetLineIndex >= lines.Count)
        {
            SetCaret(_strLen, select);
            return;
        }

        SetCaret(FindIndexClosestToX(lines[targetLineIndex], _goalColumnX, canvas), select);
    }

    private int FindLineIndex(IReadOnlyList<Range> lines, int caret)
    {
        for (var i = 0; i < lines.Count; i++)
        {
            var end = lines[i].End.Value;
            if (caret < end)
                return i;

            // A caret sitting exactly on a soft-wrap boundary (this line's end == the next
            // line's start) belongs to the next visual line; a '\n' break leaves a gap, so
            // the caret stays on this line.
            if (caret == end)
            {
                var isSoftWrapBoundary = i + 1 < lines.Count && lines[i + 1].Start.Value == end;
                if (!isSoftWrapBoundary)
                    return i;
            }
        }
        return lines.Count - 1;
    }

    private int FindIndexClosestToX(Range line, float targetX, ICanvas canvas)
    {
        var start = line.Start.Value;
        var end = line.End.Value;
        var text = Text;

        var bestIndex = start;
        var bestDistance = targetX;
        var i = start;
        while (i < end)
        {
            var next = TextBoundaries.Next(text, i);
            if (next <= i)
                break;

            var x = canvas.MeasureTextWidth(_buffer.AsSpan(start, next - start), _textStyle);
            var distance = Math.Abs(x - targetX);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestIndex = next;
            }
            else if (x > targetX)
            {
                break;
            }
            i = next;
        }
        return bestIndex;
    }

    // Every computed caret index funnels through here (clicks, vertical movement), so a pixel
    // position that lands inside a cluster is snapped once rather than at each call site.
    private void SetCaret(int index, bool select)
    {
        _caretIndex = TextBoundaries.Snap(Text, index);
        if (!select)
            _selectionStartIndex = _caretIndex;
    }

    public void Delete()
    {
        _goalColumnX = -1f;
        DropComposition();
        if (_strLen > 0)
        {
            if (IsSelecting)
            {
                DeleteSelection();
            }
            else if (_caretIndex > 0)
            {
                var clusterStart = TextBoundaries.Prev(Text, _caretIndex);
                DeleteRange(clusterStart, _caretIndex);
                _caretIndex = clusterStart;
                _selectionStartIndex = _caretIndex;
            }
        }
        SetDirty();
        SyncText();
    }

    public void DeleteWord()
    {
        _goalColumnX = -1f;
        DropComposition();
        if (_strLen == 0)
        {
            return;
        }

        if (IsSelecting)
        {
            DeleteSelection();
        }
        else if (_caretIndex > 0)
        {
            var wordStart = FindPreviousWordBoundary(_caretIndex);
            DeleteRange(wordStart, _caretIndex);
            _caretIndex = wordStart;
            _selectionStartIndex = _caretIndex;
        }
        SetDirty();
        SyncText();
    }

    public void Enter(char c)
    {
        _goalColumnX = -1f;
        DropComposition();
        if (IsSelecting)
        {
            DeleteSelection();
        }

        InsertChar(_caretIndex, c);
        _caretIndex++;
        _selectionStartIndex = _caretIndex;
        SetDirty();
        SyncText();
    }

    public void Enter(ReadOnlySpan<char> text)
    {
        _goalColumnX = -1f;
        DropComposition();
        if (_caretIndex != _selectionStartIndex)
        {
            DeleteSelection();
        }

        EnsureCapacity(_strLen + text.Length);

        var textEnd = _buffer.AsSpan(_caretIndex, _strLen - _caretIndex);
        textEnd.CopyTo(_buffer.AsSpan(_caretIndex + text.Length));

        var dst = _buffer.AsSpan(_caretIndex, text.Length);
        text.CopyTo(dst);

        _strLen += text.Length;
        _caretIndex += text.Length;
        _selectionStartIndex = _caretIndex;
        SetDirty();
        SyncText();
    }

    public string? GetSelectedText()
    {
        if (_caretIndex == _selectionStartIndex)
            return null;
        
        var min = _selectionStartIndex;
        var max = _caretIndex;
        if (min > max)
        {
            (min, max) = (max, min);
        }

        var length = max - min;
        return new string(_buffer, min, length);
    }

    public void Clear()
    {
        _goalColumnX = -1f;
        DropComposition();
        _strLen = 0;
        _caretIndex = 0;
        _selectionStartIndex = 0;
        SetDirty();
        SyncText();
    }

    /// <summary>
    /// Replaces the entire buffer with <paramref name="text"/> in a single edit — one
    /// <see cref="TextValue"/> notification — and moves the caret to the end. Use for
    /// programmatic, wholesale replacement (data binding, async load, transforms) where the
    /// intermediate empty buffer of a <see cref="Clear"/>-then-<see cref="Enter(System.ReadOnlySpan{char})"/>
    /// must not be observable.
    /// </summary>
    public void SetText(ReadOnlySpan<char> text)
    {
        _goalColumnX = -1f;
        DropComposition();
        EnsureCapacity(text.Length);
        text.CopyTo(_buffer);
        _strLen = text.Length;
        _caretIndex = _strLen;
        _selectionStartIndex = _caretIndex;
        SetDirty();
        SyncText();
    }

    // Every buffer mutation drops the composition, which keeps _composed from going stale against a
    // _buffer/_caretIndex that moved under it. For committed text that is also the correct semantics
    // — and the IME can deliver the commit either side of the empty preedit that ends the
    // composition, so this does not depend on that order. Unlike ClearComposition it is called from
    // edits that already invalidate and redraw, so it does neither itself.
    private void DropComposition()
    {
        _preedit = PreeditText.Empty;
        _composedLen = 0;
    }

    // Publishes the buffer as the observable text value. The State equality guard collapses
    // no-op edits (e.g. a Delete that removed nothing) into nothing.
    private void SyncText()
    {
        InvalidateLines();
        _text.Value = new string(_buffer, 0, _strLen);
    }

    private void InvalidateLines() => _version++;
}