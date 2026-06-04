using ZGF.Geometry;
using ZGF.Observable;

namespace ZGF.Gui;

public sealed class TextInputView : MultiChildView
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
    private readonly char[] _buffer;
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

    public TextInputView()
    {
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

        var xOffset = point.X - Position.Left + _scrollOffsetX;
        var canvas = Context!.Canvas;
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
                for (var i = lineStartIndex+1; i <= line.End.Value; i++)
                {
                    var c = _buffer.AsSpan(i-1, 1);
                    var charWidth = canvas.MeasureTextWidth(c, _textStyle);
                    var leftPart = _buffer.AsSpan(lineStartIndex, i - lineStartIndex);
                    var leftPartWidth = canvas.MeasureTextWidth(leftPart, _textStyle);
                    selectionWidth = leftPartWidth - charWidth * 0.5f;
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

    private void InsertChar(int index, char c)
    {
        _strLen++;
        for (var i = _strLen - 1; i > index; i--)
        {
            _buffer[i] = _buffer[i-1];
        }
        _buffer[index] = c;
    }

    public override float MeasureHeight(float availableWidth)
    {
        var canvas = Context?.Canvas;
        if (canvas == null)
            return 0f;

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

        var caretX = c.MeasureTextWidth(_buffer.AsSpan(0, _caretIndex), _textStyle);
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

    protected override void OnDrawSelf(ICanvas c)
    {
        var position = Position;

        DrawBackground(position, c);

        UpdateScrollOffset(position, c);

        c.PushClip(position);

        if (_isEditing && _selectionStartIndex != _caretIndex)
        {
            DrawSelectionBox(position, c);
        }

        if (_strLen == 0)
        {
            DrawPlaceholder(position, c);
        }
        else
        {
            DrawText(position, c);
        }

        if (_isEditing)
        {
            DrawCaret(position, c);
        }

        c.PopClip();
    }

    private void DrawPlaceholder(in RectF position, ICanvas c)
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
                Bottom = position.Top - lineHeight,
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

    private void DrawSelectionBox(in RectF position, ICanvas c)
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
        var minText = _buffer.AsSpan(startIndex, min - startIndex);
        var minTextWidth = c.MeasureTextWidth(minText, _textStyle);
        var startPointLeft = position.Left + minTextWidth - _scrollOffsetX;
        var startPointBottom = position.Top - linesCount * lineHeight;
        var width = position.Width - minTextWidth;

        startIndex = min;
        for (var i = min; i < max && TextWrap == Gui.TextWrap.Wrap; i++)
        {
            if (_buffer[i] == '\n' || ShouldWrap(startIndex, i+1, c, width))
            {
                var selectionRect = new RectF
                {
                    Left = startPointLeft,
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
            var text = _buffer.AsSpan(startIndex, max - startIndex);
            var textWidth = c.MeasureTextWidth(text, _textStyle);
            var selectionRect = new RectF
            {
                Left = startPointLeft,
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

    private void DrawText(in RectF position, ICanvas c)
    {
        var lineHeight = c.MeasureTextLineHeight(_textStyle);
        var left = position.Left - _scrollOffsetX;
        var bottom = position.Top - lineHeight;
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
    
    private void DrawCaret(in RectF position, ICanvas canvas)
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
        var lineText = _buffer.AsSpan(startIndex, _caretIndex - startIndex);
        var cursorPosLeft = canvas.MeasureTextWidth(lineText, _textStyle);
        var cursorPosBottom = position.Top - linesCount * lineHeight - cursorHeight;
        
        var cursorPos = new RectF
        {
            Bottom = cursorPosBottom,
            Left = position.Left + cursorPosLeft - _scrollOffsetX,
            Width = CaretWidth,
            Height = cursorHeight
        };
            
        canvas.DrawRect(new DrawRectInputs
        {
            Position = cursorPos,
            Style = _cursorStyle,
            ZIndex = GetDrawZIndex()
        });
    }
    
    public void MoveCaretTo(PointF point, bool isSelecting = false)
    {
        _caretIndex = GetCaretIndexFromPoint(point);
        if (!isSelecting)
        {
            _selectionStartIndex = _caretIndex;
        }
    }

    public void SelectAll()
    {
        _selectionStartIndex = 0;
        _caretIndex = _strLen;
    }

    public void MoveCaretLeft(bool select = false)
    {
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
        _caretIndex = FindPreviousWordBoundary(_caretIndex);
        if (!select)
            _selectionStartIndex = _caretIndex;
    }

    public void MoveCaretRightWord(bool select = false)
    {
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

    public void MoveCaretDown(bool select = false)
    {
        var currIndex = _caretIndex;
        var nextLineStartIndex = FindNextLineStartIndex(currIndex);
        if (nextLineStartIndex >= _strLen)
        {
            SetCaret(_strLen, select);
            return;
        }

        var currentLineStartIndex = FindLineStartIndex(currIndex);
        var xOffset = currIndex - currentLineStartIndex;

        var nextLineEndIndex = FindLineEndIndex(nextLineStartIndex);
        var nextLineLength =  nextLineEndIndex - nextLineStartIndex;

        if (xOffset > nextLineLength)
        {
            SetCaret(nextLineEndIndex, select);
            return;
        }

        SetCaret(nextLineStartIndex + xOffset, select);
    }

    public void MoveCaretUp(bool select = false)
    {
        var currIndex = _caretIndex;
        var currentLineStartIndex = FindLineStartIndex(currIndex);
        if (currentLineStartIndex == 0)
        {
            SetCaret(0, select);
            return;
        }

        var xOffset = currIndex - currentLineStartIndex;

        var prevLineEndIndex = currentLineStartIndex - 1;
        var prevLineStartIndex = FindLineStartIndex(prevLineEndIndex);
        var prevLineLength = prevLineEndIndex - prevLineStartIndex;

        if (xOffset > prevLineLength)
        {
            SetCaret(prevLineEndIndex, select);
            return;
        }

        SetCaret(prevLineStartIndex + xOffset, select);
    }

    private void SetCaret(int index, bool select)
    {
        _caretIndex = index;
        if (!select)
            _selectionStartIndex = _caretIndex;
    }

    private int FindLineEndIndex(int currIndex)
    {
        for (var i = currIndex; i < _strLen; i++)
        {
            if (_buffer[i] == '\n')
            {
                return i;
            }
        }
        return _strLen - 1;
    }

    private int FindLineStartIndex(int currIndex)
    {
        for (var i = currIndex - 1; i > 0; i--)
        {
            if (_buffer[i] == '\n')
            {
                return i + 1;
            }
        }
        return 0;
    }

    private int FindNextLineStartIndex(int currIndex)
    {
        for (var i = currIndex; i < _strLen; i++)
        {
            if (_buffer[i] == '\n')
            {
                return i + 1;
            }
        }
        return _strLen;
    }

    public void Delete()
    {
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
        if (_caretIndex != _selectionStartIndex)
        {
            DeleteSelection();
        }

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
        var length = Math.Min(text.Length, _buffer.Length);
        text[..length].CopyTo(_buffer);
        _strLen = length;
        _caretIndex = _strLen;
        _selectionStartIndex = _caretIndex;
        SetDirty();
        SyncText();
    }

    // Publishes the buffer as the observable text value. The State equality guard collapses
    // no-op edits (e.g. a Delete that removed nothing) into nothing.
    private void SyncText() => _text.Value = new string(_buffer, 0, _strLen);
}