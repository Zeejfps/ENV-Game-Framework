namespace ZGF.Gui;

public sealed class Label : Component
{
    private TextStyle _style = new();

    private string _text;

    public StyleValue<TextAlignment> VerticalTextAlignment
    {
        get => _style.VerticalAlignment;
        set => SetField(ref _style.VerticalAlignment, value);
    }
    
    public Label(string text)
    {
        _text = text;
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