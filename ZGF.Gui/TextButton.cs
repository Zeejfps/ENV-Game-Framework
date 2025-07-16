namespace ZGF.Gui;

public sealed class TextButton : Component, IHoverable, IMouseFocusable
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
    }

    protected override void OnAttachedToContext(Context context)
    {
        base.OnAttachedToContext(context);
        context.MouseInputSystem.EnableHover(this, this);
    }

    protected override void OnDetachedFromContext(Context prevContext)
    {
        prevContext.MouseInputSystem.DisableHover(this);
        base.OnDetachedFromContext(prevContext);
    }

    public void HandleMouseEnterEvent()
    {
        _background.Style.Apply(BackgroundHoveredStyle);
        Context?.MouseInputSystem.TryFocus(this, this);
    }

    public void HandleMouseExitEvent()
    {
        _background.Style.Apply(BackgroundNormalStyle);
        Context?.MouseInputSystem.Blur(this);
    }

    public void HandleMouseButtonEvent(in MouseButtonEvent e)
    {
    }

    public void HandleMouseWheelEvent()
    {
    }

    public void HandleMouseMoveEvent(in MouseMoveEvent e)
    {
    }
}