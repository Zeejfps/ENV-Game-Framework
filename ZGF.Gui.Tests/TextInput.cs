using ZGF.Geometry;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Tests;

public sealed class TextInput : Component
{
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
    
    private readonly RectStyle _background = new();
    private readonly TextStyle _textStyle = new();
    private readonly RectStyle _cursorStyle = new();
    private readonly RectStyle _selectionRectStyle = new();

    private int _caretIndex;
    private int _strLen;
    private bool _isEditing;
    private int _selectionStartIndex;
    private char[] _buffer;

    public TextInput()
    {
        _buffer = new char[256];

        // Default Styles
        _background.BackgroundColor = 0xEFEFEF;
        _textStyle.VerticalAlignment = TextAlignment.Center;
        _selectionRectStyle.BackgroundColor = 0x8aadff;
        
        IsInteractable = true;
    }

    protected override void OnMouseEnter()
    {
        RequestFocus();
    }

    protected override void OnMouseExit()
    {
        if (!_isEditing)
            Blur();
    }

    protected override void OnFocusLost()
    {
        _isEditing = false;
    }

    public override bool CanReleaseFocus()
    {
        return !_isEditing;
    }

    protected override bool OnMouseMoved(MouseMoveEvent e)
    {
        var isLeftMouseButtonPressed = Context!.InputSystem.IsMouseButtonPressed(MouseButton.Left);
        if (!isLeftMouseButtonPressed)
            return false;

        _caretIndex = GetCaretIndexFromPoint(e.MousePoint);
        return true;
    }

    protected override bool OnMouseButtonStateChanged(MouseButtonEvent e)
    {
        if (e.State == InputState.Pressed && e.Button == MouseButton.Left)
        {
            var position = Position;
            var containsPoint = position.ContainsPoint(e.MousePoint);

            if (_isEditing && !containsPoint)
            {
                FinishEditing();
                return false;
            }

            if (!_isEditing && containsPoint)
            {
                StartEditing();
            }
            
            var mousePoint = e.MousePoint;
            _caretIndex = GetCaretIndexFromPoint(mousePoint);
            _selectionStartIndex = _caretIndex;
        }
        return base.OnMouseButtonStateChanged(e);
    }

    private void StartEditing()
    {
        _isEditing = true;
    }
    
    private void FinishEditing()
    {
        _isEditing = false;
        Blur();
    }

    private int GetCaretIndexFromPoint(in PointF point)
    {
        if (_strLen == 0)
            return 0;
        
        var deltaX = point.X - Position.Left;
        var textMeasurer = Context!.TextMeasurer;
        for (var i = 0; i < _strLen; i++)
        {
            var firstPart = _buffer.AsSpan(0, i);
            var secondPart = _buffer.AsSpan(i, 1);
            var w = textMeasurer.MeasureTextWidth(firstPart, _textStyle) +
                    textMeasurer.MeasureTextWidth(secondPart, _textStyle) * 0.5f;
            
            if (w > deltaX)
            {
                return i;
            }
        }
        
        return _strLen;
    }

    protected override bool OnKeyboardKeyStateChanged(in KeyboardKeyEvent e)
    {
        if (!_isEditing)
            return false;

        if (e.State == InputState.Pressed)
        {
            if (e.Key == KeyboardKey.A && e.Modifiers.HasFlag(InputModifiers.Control))
            {
                _selectionStartIndex = 0;
                _caretIndex = _strLen;
                return true;
            }
            
            var isShiftPressed = (e.Modifiers & InputModifiers.Shift) > 0;
            var isSelecting = _caretIndex != _selectionStartIndex;
            if (e.Key == KeyboardKey.LeftArrow)
            {
                if (!isSelecting || isShiftPressed)
                {
                    _caretIndex--;
                    if (_caretIndex < 0)
                        _caretIndex = 0;
                }
                
                if (!isShiftPressed)
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
                
                return true;
            }
            
            if (e.Key == KeyboardKey.RightArrow)
            {
                if (!isSelecting || isShiftPressed)
                {
                    _caretIndex++;
                    if (_caretIndex > _strLen)
                        _caretIndex = _strLen;
                }
                
                if (!isShiftPressed)
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
                
                return true;
            }
            
            if (e.Key == KeyboardKey.Backspace)
            {
                if (_strLen > 0)
                {
                    if (_caretIndex != _selectionStartIndex)
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
                return true;
            }

            var c = e.Key.ToChar(isShiftPressed);
            if (c == '\0')
            {
                return true;
            }

            if (_caretIndex != _selectionStartIndex)
            {
                DeleteSelection();
            }
            
            InsertChar(_caretIndex, c);
            _caretIndex++;
            _selectionStartIndex = _caretIndex;
            return true;
        }
        
        return base.OnKeyboardKeyStateChanged(e);
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

    protected override void OnDrawSelf(ICanvas c)
    {
        var position = Position;
        
        DrawBackground(position, c);
        
        if (_isEditing && _selectionStartIndex != _caretIndex)
        {
            DrawSelectionBox(position, c);
        }

        DrawText(position, c);
        
        if (_isEditing)
        {
            DrawCaret(position, c);
        }
    }

    private void DrawBackground(in RectF position, ICanvas c)
    {
        c.AddCommand(new DrawRectCommand
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
            
        var minText = _buffer.AsSpan(0, min);
        var startPos = Context!.TextMeasurer.MeasureTextWidth(minText, _textStyle);
            
        var maxText = _buffer.AsSpan(0, max);
        var endPos = Context!.TextMeasurer.MeasureTextWidth(maxText, _textStyle);
            
        var selectionRect = new RectF
        {
            Left = position.Left + startPos,
            Bottom = position.Bottom,
            Width = endPos - startPos,
            Height = position.Height
        };  
            
        c.AddCommand(new DrawRectCommand
        {
            Position = selectionRect,
            Style = _selectionRectStyle,
            ZIndex = ZIndex
        });
    }

    private void DrawText(in RectF position, ICanvas c)
    {
        c.AddCommand(new DrawTextCommand
        {
            Position = position,
            Text = new string(_buffer, 0, _strLen),
            Style = _textStyle,
            ZIndex = ZIndex
        });   
    }

    private void DrawCaret(in RectF position, ICanvas c)
    {
        var textToMeasure = _buffer.AsSpan(0, _caretIndex);
        var cursorPosLeft = Context!.TextMeasurer.MeasureTextWidth(textToMeasure, _textStyle);

        var cursorHeight = position.Height - 6f;
        var cursorPos = new RectF
        {
            Bottom = position.Bottom + 2f,
            Left = position.Left + cursorPosLeft,
            Width = 2,
            Height = cursorHeight
        };
            
        c.AddCommand(new DrawRectCommand
        {
            Position = cursorPos,
            Style = _cursorStyle,
            ZIndex = ZIndex
        });
    }
}