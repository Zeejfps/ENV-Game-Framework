namespace ZGF.Gui.Tests;

public sealed class ContextMenuItem : Component
{
    private readonly ContextMenu _contextMenu;
    private readonly Panel _bg;

    private ContextMenuManager? ContextMenuManager => Get<ContextMenuManager>();

    public ContextMenuItem(ContextMenu contextMenu, string name)
    {
        _contextMenu = contextMenu;
        _bg = new Panel
        {
            BackgroundColor = 0xDEDEDE,
            Padding = PaddingStyle.All(4)
        };
        _bg.Add(new Label(name));
        ZIndex = 2;
        Add(_bg);
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
        _bg.BackgroundColor = 0x9C9CCE;
        ContextMenuManager?.SetKeepOpen(_contextMenu);
    }

    protected override void OnMouseExit()
    {
        _bg.BackgroundColor = 0xDEDEDE;
        ContextMenuManager?.HideContextMenu(_contextMenu);
    }
}