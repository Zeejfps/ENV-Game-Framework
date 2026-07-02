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
        set => SetField(ref _textWrap, value);
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
        set => SetField(ref _textStyle.FontSize, value);
    }

    public StyleValue<FontFeatureSet> FontFeatures
    {
        get => _textStyle.FontFeatures;
        set => SetField(ref _textStyle.FontFeatures, value);
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

    private const float CaretWidth = 2f;

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

    /// <summary>Zero-allocation view of the current text. Prefer this for renderers,
    /// controllers, and equality checks; use <see cref="TextValue"/> to observe changes.</summary>
    public ReadOnlySpan<char> Text => _buffer.AsSpan(0, _strLen);

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
                var selectionWidth = 0f;
                var singleLine = TextWrap != Gui.TextWrap.Wrap;
                for (var i = lineStartIndex+1; i <= line.End.Value; i++)
                {
                    // Boundary x is the midpoint of char i-1: clicking past it selects to its right.
                    // Single line measures in context (cursive-correct); wrapped lines measure the
                    // per-segment substring as before.
                    float leftPartWidth, prevPartWidth;
                    if (singleLine)
                    {
                        leftPartWidth = canvas.MeasureTextPrefix(Text, i, _textStyle);
                        prevPartWidth = canvas.MeasureTextPrefix(Text, i - 1, _textStyle);
                    }
                    else
                    {
                        leftPartWidth = canvas.MeasureTextWidth(_buffer.AsSpan(lineStartIndex, i - lineStartIndex), _textStyle);
                        prevPartWidth = leftPartWidth - canvas.MeasureTextWidth(_buffer.AsSpan(i - 1, 1), _textStyle);
                    }
                    selectionWidth = (leftPartWidth + prevPartWidth) * 0.5f;
                    if (selectionWidth > xOffset)
                    {
                        return i - 1;
                    }
                }
                
                if (xOffset > selectionWidth)
                {
                    return lineEndIndex;
                }
                        
                return lineEndIndex - 1;
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

    private void DeleteChar(int index)
    {
        if (index < _strLen)
        {
            for (var i = index; i < _strLen; i++)
            {
                _buffer[i] = _buffer[i + 1];
            }
        }
        _strLen--;
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
        if (_strLen == 0)
            return lineHeight;

        // availableWidth <= 0 means "unconstrained" — fall back to intrinsic width so we
        // still report a sensible (single-line) height instead of one line per character.
        var width = availableWidth > 0f ? availableWidth : MeasureWidth();
        var height = GetLines(width, canvas).Count() * lineHeight;
        if (Height.IsSet && height < Height)
            return Height;

        return height;
    }

    private IEnumerable<Range> GetLines(float width, ICanvas canvas)
    {
        var startIndex = 0;
        for (var i = 0; i < _strLen; i++)
        {
            var range = new Range(startIndex, i);
            var line = _buffer.AsSpan(range);
            if (TextWrap == Gui.TextWrap.Wrap)
            {
                if (ShouldWrap(_buffer.AsSpan(startIndex, i-startIndex + 1), canvas, width))
                {
                    yield return range;
                    startIndex = i;
                }
                else if (_buffer[i] == '\n')
                {
                    yield return range;
                    startIndex = i+1;
                }
            }
        }

        if (startIndex <= _strLen)
        {
            yield return new Range(startIndex, _strLen);
        }
    }

    private void UpdateScrollOffset(in RectF position, ICanvas c)
    {
        if (TextWrap == Gui.TextWrap.Wrap)
        {
            _scrollOffsetX = 0f;
            return;
        }

        var caretX = c.MeasureTextPrefix(Text, _caretIndex, _textStyle);
        var width = position.Width;

        if (caretX - _scrollOffsetX < 0f)
        {
            _scrollOffsetX = caretX;
        }
        else if (caretX - _scrollOffsetX > width - CaretWidth)
        {
            _scrollOffsetX = caretX - width + CaretWidth;
        }

        var totalWidth = c.MeasureTextWidth(_buffer.AsSpan(0, _strLen), _textStyle);
        var maxOffset = Math.Max(0f, totalWidth - width + CaretWidth);
        _scrollOffsetX = Math.Clamp(_scrollOffsetX, 0f, maxOffset);
    }

    // The field is RTL when its first strong character is RTL; an empty field follows the UI
    // direction so the caret rests on the correct side before anything is typed.
    private bool ResolveContentRtl()
    {
        var text = Text;
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

        if (_isEditing && _selectionStartIndex != _caretIndex)
        {
            DrawSelectionBox(position, c, verticalOffset);
        }

        if (_strLen == 0)
        {
            DrawPlaceholder(position, c, verticalOffset);
        }
        else
        {
            DrawText(position, c, verticalOffset);
        }

        if (_isEditing)
        {
            DrawCaret(position, c, verticalOffset);
        }

        c.PopClip();
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
        var min = _selectionStartIndex;
        var max = _caretIndex;
        if (_selectionStartIndex > _caretIndex)
        {
            min = _caretIndex;
            max = _selectionStartIndex;
        }
        
        var startIndex = 0;
        var linesCount = 1;

        if (TextWrap == Gui.TextWrap.Wrap)
        {
            for (var i = 0; i < min; i++)
            {
                if (_buffer[i] == '\n')
                {
                    startIndex = i;
                    linesCount++;
                } 
                else if (ShouldWrap(startIndex, i+1, c, position.Width))
                {
                    startIndex = i;
                    linesCount++;
                }
            }
        }

        var lineHeight = c.MeasureTextLineHeight(_textStyle);
        // Single line: in-context prefix width (cursive-correct); wrapped lines keep the segment measure.
        var minTextWidth = TextWrap != Gui.TextWrap.Wrap
            ? c.MeasureTextPrefix(Text, min, _textStyle)
            : c.MeasureTextWidth(_buffer.AsSpan(startIndex, min - startIndex), _textStyle);
        var startPointLeft = position.Left + minTextWidth - _scrollOffsetX;
        var startPointBottom = position.Top - linesCount * lineHeight + verticalOffset;
        var width = position.Width - minTextWidth;

        startIndex = min;
        for (var i = min; i < max && TextWrap == Gui.TextWrap.Wrap; i++)
        {
            if (_buffer[i] == '\n' || ShouldWrap(startIndex, i+1, c, width))
            {
                var selectionRect = new RectF
                {
                    // Mirror the segment within the field for RTL, so the highlight lands on the
                    // right-aligned text (scroll is 0 for wrapped lines).
                    Left = _contentRtl ? position.Left + position.Right - startPointLeft - width : startPointLeft,
                    Bottom = startPointBottom,
                    Width = width,
                    Height = lineHeight
                };

                c.DrawRect(new DrawRectInputs
                {
                    Position = selectionRect,
                    Style = _selectionRectStyle,
                    ZIndex = GetDrawZIndex()
                });

                startIndex = i;
                startPointLeft = position.Left;
                startPointBottom -= lineHeight;
                width = position.Width;
            }
        }
        
        if (startIndex <= max)
        {
            var textWidth = TextWrap != Gui.TextWrap.Wrap
                ? c.MeasureTextPrefix(Text, max, _textStyle) - minTextWidth
                : c.MeasureTextWidth(_buffer.AsSpan(startIndex, max - startIndex), _textStyle);
            // Mirror the final segment for RTL (single-line and the last wrapped line alike): reflect
            // its extent within the field so it sits on the right-aligned text.
            var segLeft = _contentRtl
                ? position.Left + position.Right - startPointLeft - textWidth
                : startPointLeft;
            var selectionRect = new RectF
            {
                Left = segLeft,
                Bottom = startPointBottom,
                Width = textWidth,
                Height = lineHeight
            };
            
            c.DrawRect(new DrawRectInputs
            {
                Position = selectionRect,
                Style = _selectionRectStyle,
                ZIndex = GetDrawZIndex()
            });
        }
    }

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
                Text = _buffer.AsSpan(line).ToString(),
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

        var lineCount = _strLen == 0 ? 1 : GetLines(position.Width, c).Count();
        var slack = position.Height - lineCount * lineHeight;
        return slack > 0f ? -slack * 0.5f : 0f;
    }

    private bool ShouldWrap(int startIndex, int endIndex, ICanvas canvas, float maxWidth)
    {
        var line = _buffer.AsSpan(startIndex, endIndex - startIndex);
        return ShouldWrap(line, canvas, maxWidth);
    }

    private bool ShouldWrap(ReadOnlySpan<char> line, ICanvas canvas, float maxWidth)
    {
        var lineWidth = canvas.MeasureTextWidth(line, _textStyle);
        return lineWidth >= maxWidth;
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
        var startIndex = 0;
        var linesCount = 0;
        for (var i = 0; i < _caretIndex; i++)
        {
            if (TextWrap == Gui.TextWrap.Wrap)
            {
                if (ShouldWrap(startIndex, i+1, canvas, position.Width))
                {
                    startIndex = i;
                    linesCount++;
                }
                else if (_buffer[i] == '\n')
                {
                    startIndex = i;
                    linesCount++;
                }
            }
        }

        var lineHeight = canvas.MeasureTextLineHeight(_textStyle);
        var cursorHeight = lineHeight;
        // Single line: measure the caret prefix in the context of the whole line, so a cursive
        // Arabic caret lands correctly and a zero-width mark doesn't shift it. (Wrapped lines keep
        // the per-segment measure.)
        var cursorPosLeft = TextWrap != Gui.TextWrap.Wrap
            ? canvas.MeasureTextPrefix(Text, _caretIndex, _textStyle)
            : canvas.MeasureTextWidth(_buffer.AsSpan(startIndex, _caretIndex - startIndex), _textStyle);
        var cursorPosBottom = position.Top - linesCount * lineHeight - cursorHeight + verticalOffset;

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
        _caretIndex = GetCaretIndexFromPoint(point);
        if (!isSelecting)
        {
            _selectionStartIndex = _caretIndex;
        }
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
            _caretIndex--;
            if (_caretIndex < 0)
                _caretIndex = 0;
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
            _caretIndex++;
            if (_caretIndex > _strLen)
                _caretIndex = _strLen;
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

    private static bool IsWordChar(char c) => char.IsLetterOrDigit(c) || c == '_';

    private int FindPreviousWordBoundary(int index)
    {
        var i = index;
        while (i > 0 && !IsWordChar(_buffer[i - 1]))
            i--;
        while (i > 0 && IsWordChar(_buffer[i - 1]))
            i--;
        return i;
    }

    private int FindNextWordBoundary(int index)
    {
        var i = index;
        while (i < _strLen && IsWordChar(_buffer[i]))
            i++;
        while (i < _strLen && !IsWordChar(_buffer[i]))
            i++;
        return i;
    }

    public void MoveCaretDown(bool select = false) => MoveCaretVertically(1, select);

    public void MoveCaretUp(bool select = false) => MoveCaretVertically(-1, select);

    // Vertical movement walks the same visual lines GetLines/DrawText produce (hard '\n'
    // breaks plus soft wraps), and tracks the caret's pixel x so the column is preserved
    // across proportional fonts rather than by character count.
    private void MoveCaretVertically(int direction, bool select)
    {
        var canvas = _canvas;

        var lines = GetLines(Position.Width, canvas).ToList();
        var lineIndex = FindCaretLineIndex(lines);

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

    private int FindCaretLineIndex(List<Range> lines)
    {
        for (var i = 0; i < lines.Count; i++)
        {
            var end = lines[i].End.Value;
            if (_caretIndex < end)
                return i;

            // A caret sitting exactly on a soft-wrap boundary (this line's end == the next
            // line's start) belongs to the next visual line; a '\n' break leaves a gap, so
            // the caret stays on this line.
            if (_caretIndex == end)
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

        var bestIndex = start;
        var bestDistance = targetX;
        for (var i = start + 1; i <= end; i++)
        {
            var x = canvas.MeasureTextWidth(_buffer.AsSpan(start, i - start), _textStyle);
            var distance = Math.Abs(x - targetX);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestIndex = i;
            }
            else if (x > targetX)
            {
                break;
            }
        }
        return bestIndex;
    }

    private void SetCaret(int index, bool select)
    {
        _caretIndex = index;
        if (!select)
            _selectionStartIndex = _caretIndex;
    }

    public void Delete()
    {
        _goalColumnX = -1f;
        if (_strLen > 0)
        {
            if (IsSelecting)
            {
                DeleteSelection();
            }
            else if (_caretIndex > 0)
            {
                DeleteChar(_caretIndex - 1);
                _caretIndex--;
                _selectionStartIndex = _caretIndex;
            }
        }
        SetDirty();
        SyncText();
    }

    public void DeleteWord()
    {
        _goalColumnX = -1f;
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
        EnsureCapacity(text.Length);
        text.CopyTo(_buffer);
        _strLen = text.Length;
        _caretIndex = _strLen;
        _selectionStartIndex = _caretIndex;
        SetDirty();
        SyncText();
    }

    // Publishes the buffer as the observable text value. The State equality guard collapses
    // no-op edits (e.g. a Delete that removed nothing) into nothing.
    private void SyncText() => _text.Value = new string(_buffer, 0, _strLen);
}