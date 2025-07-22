using System.Text;
using ZGF.Geometry;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Tests;

public sealed class TextInput : Component
{
    private readonly RectStyle _background = new();
    private readonly TextStyle _textStyle = new();
    private readonly RectStyle _cursorStyle = new();
    private readonly RectStyle _selectionRectStyle = new();

    private int _caretIndex;
    private char[] _buffer;
    private int _strLen;
    private bool _isEditing;
    private int _selectionStartIndex;

    public TextInput()
    {
        _buffer = new char[256];

        _background.BackgroundColor = 0xEFEFEF;
        _textStyle.VerticalAlignment = TextAlignment.Center;

        _selectionRectStyle.BackgroundColor = 0x5797ff;
        
        IsInteractable = true;
    }

    protected override void OnMouseEnter()
    {
        Console.WriteLine("OnMouseEnter - TextInput");
        RequestFocus();
    }

    protected override void OnMouseExit()
    {
        Console.WriteLine("OnMouseExit - TextInput");
        if (!_isEditing)
        {
            Blur();
        }
    }

    protected override void OnFocusLost()
    {
        _isEditing = false;
    }

    public override bool CanReleaseFocus()
    {
        return !_isEditing;
    }

    protected override bool OnMouseButtonStateChanged(MouseButtonEvent e)
    {
        if (e.State == InputState.Pressed)
        {
            var position = Position;
            var containsPoint = position.ContainsPoint(e.Position);

            if (_isEditing && !containsPoint)
            {
                _isEditing = false;
                Console.WriteLine("Editing stopped");
                Blur();
                return false;
            }

            if (!_isEditing && containsPoint)
            {
                _isEditing = true;
                _caretIndex = _strLen;
                _selectionStartIndex = _caretIndex;
                Console.WriteLine("Editing Started");
            }

            if (_strLen == 0)
                return false;

            // var mousePoint = e.Position;
            //
            // var deltaX = mousePoint.X - position.Left;
            // var smallest = float.MaxValue;
            // var found = false;
            // for (var i = 0; i < _buffer.Length; i++)
            // {
            //     var text = _buffer.AsSpan(0, i+1);
            //     var w = Context.TextMeasurer.MeasureTextWidth(text, _textStyle);
            //     var dd = MathF.Abs(w - deltaX);
            //     if (deltaX < w)
            //     {
            //         _caretIndex = i - 1;
            //         found = true;
            //         break;
            //     }
            //     if (dd < smallest)
            //     {
            //         smallest = dd;
            //     }
            //     else if (dd > smallest)
            //     {
            //         _caretIndex = i - 1;
            //         found = true;
            //         break;
            //     }
            // }
            //
            // if (!found)
            // {
            //     _caretIndex = _buffer.Length - 1;
            // }
        }
        
        return base.OnMouseButtonStateChanged(e);
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

        c.AddCommand(new DrawRectCommand
        {
            Position = position,
            Style = _background,
            ZIndex = ZIndex
        });
        
        if (_isEditing)
        {
            if (_selectionStartIndex != _caretIndex)
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

        }

        c.AddCommand(new DrawTextCommand
        {
            Position = position,
            Text = new string(_buffer, 0, _strLen),
            Style = _textStyle,
            ZIndex = ZIndex
        });

        if (_isEditing)
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
}