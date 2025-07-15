namespace ZGF.Gui;

public sealed class Label : Component
{
    private TextStyle _style;
    public TextStyle Style
    {
        get => _style;
        set => SetField(ref _style, value);
    }

    private string _text;

    public Label(string text)
    {
        _text = text;
    }

    protected override void OnDraw(ICanvas c)
    {
        c.DrawText(Position, _text, Style);
    }
}