namespace ZGF.Gui;

public sealed class TextButton : Container, IMouseListener
{
    private Rect _background;

    private static RectStyle BackgroundHoveredStyle { get; } = new();
    private static RectStyle BackgroundNormalStyle { get; } = new();

    public TextButton(string text)
    {
        _background = new Rect();

        var sackLayout = new StackLayout();
        sackLayout.Add(_background);
        sackLayout.Add(new Label(text));
        Layout = sackLayout;

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