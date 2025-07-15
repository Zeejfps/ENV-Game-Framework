namespace ZGF.Gui;

public sealed class Button : Container, IMouseListener
{
    private Rect _background;
    
    private static RectStyle BackgroundHoveredStyle { get; } = new();
    private static RectStyle BackgroundNormalStyle { get; } = new();
    
    public Button()
    {
        _background = new Rect();
        
        var sackLayout = new StackLayout();
        sackLayout.Add(_background);
        sackLayout.Add(new Label("Hello World!"));
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