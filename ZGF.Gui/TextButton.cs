namespace ZGF.Gui;

public sealed class TextButton : Component, IMouseListener, ICaptureMouse
{
    private Rect _background;

    private static Style BackgroundHoveredStyle { get; } = new();
    private static Style BackgroundNormalStyle { get; } = new();

    public TextButton(string text)
    {
        _background = new Rect
        {
            Style =
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
        _background.Style.Apply(BackgroundHoveredStyle);
        CaptureMouse(this);
    }

    public void HandleMouseExitEvent()
    {
        _background.Style.Apply(BackgroundNormalStyle);
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