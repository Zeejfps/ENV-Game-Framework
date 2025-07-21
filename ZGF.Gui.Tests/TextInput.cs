using System.Text;
using ZGF.Geometry;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Tests;

public sealed class TextInput : Component
{
    private readonly RectStyle _background = new();
    private readonly TextStyle _textStyle = new();
    private readonly RectStyle _cursorStyle = new();

    private int _caretIndex;
    private char[] _buffer;
    private int _strLen;

    public TextInput()
    {
        _buffer = new char[256];

        _background.BackgroundColor = 0xEFEFEF;
        _background.BorderSize = BorderSizeStyle.All(1);
        _background.BorderColor = BorderColorStyle.All(0xff00ff);
        _textStyle.VerticalAlignment = TextAlignment.Center;

        IsInteractable = true;
    }

    protected override void OnMouseEnter()
    {
        TryFocus();
    }

    protected override void OnMouseExit()
    {
        Blur();
    }

    protected override void OnMouseButtonStateChanged(MouseButtonEvent e)
    {
        if (e.State == InputState.Pressed)
        {
            if (_strLen == 0)
                return;

            // var mousePoint = e.Position;
            // var position = Position;
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
    }

    protected override void OnKeyboardKeyStateChanged(in KeyboardKeyEvent e)
    {
        if (e.State == InputState.Pressed)
        {
            if (e.Key == KeyboardKey.LeftArrow)
            {
                _caretIndex--;
                if (_caretIndex < 0)
                    _caretIndex = 0;
            }
            else if (e.Key == KeyboardKey.RightArrow)
            {
                _caretIndex++;
                if (_caretIndex > _strLen)
                    _caretIndex = _strLen;
            }
            else if (e.Key == KeyboardKey.Backspace)
            {
                if (_strLen > 0 && _caretIndex > 0)
                {
                    DeleteChar(_caretIndex - 1);
                    _caretIndex--;
                }
            }
            else
            {
                InsertChar(_caretIndex, e.Key.ToChar());
                _caretIndex++;
            }
        }
    }

    private void DeleteChar(int index)
    {
        if (index == _strLen)
        {
        }
        _strLen--;
    }

    private void InsertChar(int index, char c)
    {
        if (index == _strLen)
        {
            _buffer[index] = c;
        }
        else
        {
            for (var i = index; i <= _strLen; i++)
            {
                _buffer[i + 1] = _buffer[i];
            }
            _buffer[index] = c;
        }
        _strLen++;
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

        c.AddCommand(new DrawTextCommand
        {
            Position = position,
            Text = new string(_buffer, 0, _strLen),
            Style = _textStyle,
            ZIndex = ZIndex
        });

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