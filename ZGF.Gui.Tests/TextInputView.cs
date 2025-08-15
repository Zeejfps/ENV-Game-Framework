using ZGF.Geometry;

namespace ZGF.Gui.Tests;

public sealed class TextInputView : View
{
    public StyleValue<TextWrap> TextWrap
    {
        get => _textStyle.TextWrap;
        set => SetField(ref _textStyle.TextWrap, value);
    }
    
    public StyleValue<bool> IsMultiLine
    {
        get => _textStyle.IsMultiLine;
        set => SetField(ref _textStyle.IsMultiLine, value);
    }
    
    public StyleValue<uint> BackgroundColor
    {
        get => _background.BackgroundColor;
        set => SetField(ref _background.BackgroundColor, value);
    }

    public StyleValue<uint> TextColor
    {
        get => _textStyle.TextColor;
        set => SetField(ref _textStyle.TextColor, value);
    }

    public StyleValue<TextAlignment> TextVerticalAlignment
    {
        get => _textStyle.VerticalAlignment;
        set => SetField(ref _textStyle.VerticalAlignment, value);
    }

    public StyleValue<uint> SelectionRectColor
    {
        get => _selectionRectStyle.BackgroundColor;
        set => SetField(ref _selectionRectStyle.BackgroundColor, value);
    }

    public StyleValue<uint> CaretColor
    {
        get => _cursorStyle.BackgroundColor;
        set => SetField(ref _cursorStyle.BackgroundColor, value);
    }

    public bool IsSelecting => _caretIndex != _selectionStartIndex;
    
    private readonly RectStyle _background = new();
    private readonly TextStyle _textStyle = new();
    private readonly RectStyle _cursorStyle = new();
    private readonly RectStyle _selectionRectStyle = new();

    private int _caretIndex;
    private int _strLen;
    private bool _isEditing;
    private int _selectionStartIndex;
    private char[] _buffer;
    
    public ReadOnlySpan<char> Text => _buffer.AsSpan(0, _strLen);
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

        var xOffset = point.X - Position.Left;
        var canvas = Context!.Canvas;
        var lineCount = 1;
        var lineHeight = canvas.MeasureTextLineHeight(_textStyle);
        // TODO: This can be improved. Currently this is a brute force linear search
        for (var i = 0; i < _strLen; i++)
        {
            if (_buffer[i] == '\n')
            {
                lineCount++;
            }
            
            var lineBottom = Position.Top - lineCount * lineHeight;
            var lineTop = lineBottom + lineHeight;
            if (point.Y < lineTop && point.Y >= lineBottom)
            {
                var x = i+1;
                for (; x <= _strLen; x++)
                {
                    var c = _buffer.AsSpan(x-1, 1);
                    var charWidth = canvas.MeasureTextWidth(c, _textStyle);
                    var leftPart = _buffer.AsSpan(i, x-i);
                    var leftPartWidth = canvas.MeasureTextWidth(leftPart, _textStyle);
                    var selectionWidth = leftPartWidth - charWidth * 0.5f;
                    
                    if (_buffer[x] == '\n')
                    {
                        if (xOffset > selectionWidth)
                        {
                            return x;
                        }
                        
                        return x - 1;
                    }
    
                    if (selectionWidth > xOffset)
                    {
                        return x - 1;
                    }
                }

                return x - 1;
            }
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

    public override float MeasureHeight()
    {
        var canvas = Context?.Canvas;
        if (canvas == null)
            return 0f;
        
        var lineHeight = canvas.MeasureTextLineHeight(_textStyle);
        if (_strLen == 0)
            return lineHeight;
        
        var width = MeasureWidth();
        var lines = GetLines(width, canvas);
        var height = 0f;
        foreach (var line in lines)
        {
            height += lineHeight;
          
        }
        
        if (PreferredHeight.IsSet && height < PreferredHeight)
            return PreferredHeight;
        
        return height;
    }

    private IEnumerable<Memory<char>> GetLines(float width, ICanvas canvas)
    {
        var startIndex = 0;
        for (var i = 0; i < _strLen; i++)
        {
            var line = new Memory<char>(_buffer, startIndex, i - startIndex);
            if (TextWrap == Gui.TextWrap.Wrap)
            {
                if (ShouldWrap(line.Span, canvas, width))
                {
                    yield return line;
                    startIndex = i;
                }
                else if (_buffer[i] == '\n')
                {
                    yield return line;
                    startIndex = i+1;
                }
            }
        }

        if (startIndex <= _strLen)
        {
            yield return new Memory<char>(_buffer, startIndex, _strLen - startIndex);
        }
    }

    protected override void OnDrawSelf(ICanvas c)
    {
        var position = Position;
        
        DrawBackground(position, c);
        
        c.PushClip(position);

        if (_isEditing && _selectionStartIndex != _caretIndex)
        {
            DrawSelectionBox(position, c);
        }

        DrawText(position, c);
        
        if (_isEditing)
        {
            DrawCaret(position, c);
        }
        
        c.PopClip();
    }

    private void DrawBackground(in RectF position, ICanvas c)
    {
        c.DrawRect(new DrawRectInputs
        {
            Position = position,
            Style = _background,
            ZIndex = ZIndex
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
        for (var i = 0; i < min; i++)
        {
            if (_buffer[i] == '\n')
            {
                startIndex = i;
                linesCount++;
            } 
        }


        var lineHeight = c.MeasureTextLineHeight(_textStyle);
        var minText = _buffer.AsSpan(startIndex, min - startIndex);
        var minTextWidth = c.MeasureTextWidth(minText, _textStyle);
        var startPointLeft = position.Left + minTextWidth;
        var startPointBottom = position.Top - linesCount * lineHeight;
        var width = position.Width - minTextWidth;

        startIndex = min;
        for (var i = min; i < max; i++)
        {
            if (_buffer[i] == '\n')
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
                    ZIndex = ZIndex
                });
                
                startIndex = i;
                startPointLeft = position.Left;
                startPointBottom -= lineHeight;
                width = position.Width;
            } 
        }
        
        if (startIndex < max)
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
                ZIndex = ZIndex
            });
        } 
    }

    private void DrawText(in RectF position, ICanvas c)
    {
        var lineHeight = c.MeasureTextLineHeight(_textStyle);
        var left = position.Left;
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
                Text = line.ToString(),
                Style = _textStyle,
                ZIndex = ZIndex
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
                if (ShouldWrap(startIndex, i, canvas, position.Width))
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
            Left = position.Left + cursorPosLeft,
            Width = 2,
            Height = cursorHeight
        };
            
        canvas.DrawRect(new DrawRectInputs
        {
            Position = cursorPos,
            Style = _cursorStyle,
            ZIndex = ZIndex
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

    public void MoveCaretDown(bool select = false)
    {
        var currIndex = _caretIndex;
        var nextLineStartIndex = FindNextLineStartIndex(currIndex);
        if (nextLineStartIndex >= _strLen)
        {
            _caretIndex = _strLen;
            _selectionStartIndex = _caretIndex;
            return;
        }

        var currentLineStartIndex = FindLineStartIndex(currIndex);
        var xOffset = currIndex - currentLineStartIndex;

        var nextLineEndIndex = FindLineEndIndex(nextLineStartIndex);
        var nextLineLength =  nextLineEndIndex - nextLineStartIndex;
        
        if (xOffset > nextLineLength)
        {
            _caretIndex = nextLineEndIndex;
            _selectionStartIndex = _caretIndex;
            return;
        }
        
        _caretIndex = nextLineStartIndex + xOffset;
        _selectionStartIndex = _caretIndex;
    }
    
    public void MoveCaretUp(bool select = false)
    {
        
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
}