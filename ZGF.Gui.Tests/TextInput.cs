using System.Text;
using ZGF.Geometry;

namespace ZGF.Gui.Tests;

public sealed class TextInput : Component
{
    private int _caretIndex;

    private readonly RectStyle _background = new();
    private readonly TextStyle _textStyle = new();
    private readonly RectStyle _cursorStyle = new();
    private readonly StringBuilder _stringBuilder;

    private char[] _buffer;

    public TextInput()
    {
        _stringBuilder = new StringBuilder();
        _buffer =
        [
            'a',
            'b',
            'c',
            'd',
            'e'
        ];
        _caretIndex = 2;

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
            var mousePoint = e.Position;
            var position = Position;
            var deltaX = mousePoint.X - position.Left;
            // for (var i = 0; i < _buffer.Length; i++)
            // {
            //     var text = _buffer.AsSpan(0, i+1);
            //     var w = Context.TextMeasurer.MeasureTextWidth(text, _textStyle);
            //
            // }

            var text = _buffer.AsSpan(0, _caretIndex+1);
            var w = Context.TextMeasurer.MeasureTextWidth(text, _textStyle);
            if (deltaX < w)
            {
                _caretIndex--;
            }
            else
            {
                _caretIndex++;
            }
        }
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
            Text = new string(_buffer),
            Style = _textStyle,
            ZIndex = ZIndex
        });

        var textToMeasure = _buffer.AsSpan(0, _caretIndex+1);
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