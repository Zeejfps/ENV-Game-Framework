namespace ZGF.Gui;

public sealed class TextView : View
{
    private readonly TextStyle _style = new();

    public StyleValue<uint> TextColor
    {
        get => _style.TextColor;
        set => SetField(ref _style.TextColor, value);
    }

    public StyleValue<TextAlignment> VerticalTextAlignment
    {
        get => _style.VerticalAlignment;
        set => SetField(ref _style.VerticalAlignment, value);
    }
    
    public StyleValue<TextAlignment> HorizontalTextAlignment
    {
        get => _style.HorizontalAlignment;
        set => SetField(ref _style.HorizontalAlignment, value);
    }

    private string? _text;
    public string? Text
    {
        get => _text;
        set => SetField(ref _text, value);
    }

    public override float MeasureWidth()
    {
        if (Context == null)
            return 0f;

        if (_text == null)
            return 0f;

        return Context.Canvas.MeasureTextWidth(_text, _style);
    }

    public override float MeasureHeight()
    {
        if (Context == null)
            return 0f;

        if (_text == null)
            return 0f;

        return Context.Canvas.MeasureTextHeight(_text, _style);
    }

    protected override void OnApplyStyle(Style style)
    {
        base.OnApplyStyle(style);
        if (style.TextColor.IsSet)
            _style.TextColor = style.TextColor;
    }

    protected override void OnDrawSelf(ICanvas c)
    {
        if (_text == null)
            return;

        c.DrawText(new DrawTextCommand
        {
            Position = Position,
            Text = _text,
            Style = _style,
            ZIndex = ZIndex
        });
    }
}