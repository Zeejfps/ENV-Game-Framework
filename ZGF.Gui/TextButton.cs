namespace ZGF.Gui;

public sealed class TextButton : Component, IMouseListener, ICaptureMouse
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

        AddMouseListener(this);
    }

    public void HandleMouseEnterEvent()
    {
        _background.Style = BackgroundHoveredStyle;
        CaptureMouse(this);
    }

    public void HandleMouseExitEvent()
    {
        _background.Style = BackgroundNormalStyle;
        ReleaseMouse(this);
    }

    public void HandleMouseButtonEvent()
    {
    }

    public void HandleMouseWheelEvent()
    {
    }

    public void HandleMouseMoveEvent()
    {
    }
}