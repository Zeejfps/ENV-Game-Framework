namespace ZGF.Gui;

public sealed class TextButton : Component, IMouseListener
{
    private Rect _background;

    private static RectStyle BackgroundHoveredStyle { get; } = new();
    private static RectStyle BackgroundNormalStyle { get; } = new();

    public TextButton(string text)
    {
        _background = new Rect();

        Add(_background);
        Add(new Label(text));

        AddMouseListener(this);
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