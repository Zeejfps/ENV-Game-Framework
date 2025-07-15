namespace ZGF.Gui;

public sealed class TextButton : Component
{
    private Rect _background;

    private static RectStyle BackgroundHoveredStyle { get; } = new();
    private static RectStyle BackgroundNormalStyle { get; } = new();

    public TextButton(string text)
    {
        _background = new Rect
        {
            Style = new RectStyle
            {
                BackgroundColor = 0x232345,
                BorderSize = BorderSizeStyle.All(4),
                //BorderColor = BorderColorStyle.All(0xFF00FF)
            }
        };

        Add(_background);
        Add(new Label(text));
    }

    public void OnMouseEnter()
    {
        _background.Style = BackgroundHoveredStyle;
    }

    public void OnMouseExit()
    {
        _background.Style = BackgroundNormalStyle;
    }
}