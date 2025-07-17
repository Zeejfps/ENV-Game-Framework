namespace ZGF.Gui.Tests;

public sealed class MenuItem : Component
{
    private readonly Panel _background;
    private readonly Label _label;

    public MenuItem(string text)
    {
        _background = new Panel
        {
            BackgroundColor = 0xDEDEDE,
            Padding = PaddingStyle.All(3)
        };
        
        _label = new Label(text)
        {
            VerticalTextAlignment = TextAlignment.Center,
        };
        
        _background.Add(_label);
        Add(_background);
    }

    protected override void OnAttachedToContext(Context context)
    {
        base.OnAttachedToContext(context);
        context.MouseInputSystem.EnableHover(this);
    }

    protected override void OnDetachedFromContext(Context context)
    {
        context.MouseInputSystem.DisableHover(this);
        base.OnDetachedFromContext(context);
    }

    protected override void OnMouseEnter()
    {
        _background.BackgroundColor = 0x9C9CCE;
    }

    protected override void OnMouseExit()
    {
        _background.BackgroundColor = 0xDEDEDE;
    }
}