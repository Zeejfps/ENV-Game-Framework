namespace ZGF.Gui;

public sealed class Label : Component
{
    private readonly TextStyle _style = new();

    private string _text;

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
    
    public Label(string text)
    {
        _text = text;
    }

    public override float MeasureWidth()
    {
        return _text.Length * 8f;
    }

    protected override void OnDrawSelf(ICanvas c)
    {
        c.AddCommand(new DrawTextCommand
        {
            Position = Position,
            Text = _text,
            Style = _style
        });
    }
}