namespace ZGF.Gui.Tests;

public sealed class MenuItem : Component
{
    private readonly Panel _background;
    private readonly Label _label;

    private ContextMenuManager? ContextMenuManager { get; set; }
    
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
        ContextMenuManager = context.Get<ContextMenuManager>();
        context.MouseInputSystem.EnableHover(this);
    }

    protected override void OnDetachedFromContext(Context context)
    {
        ContextMenuManager = null;
        context.MouseInputSystem.DisableHover(this);
        base.OnDetachedFromContext(context);
    }

    protected override void OnMouseEnter()
    {
        _background.BackgroundColor = 0x9C9CCE;
        ShowMenu();
    }

    protected override void OnMouseExit()
    {
        _background.BackgroundColor = 0xDEDEDE;
        HideMenu();
    }

    private void ShowMenu()
    {
        if (Context == null)
            return;

        _contextMenu = ContextMenuManager?.ShowContextMenu(Position.BottomLeft);
    }

    private void HideMenu()
    {
        if (Context == null)
            return;
        
        if (_contextMenu != null)
        {
            ContextMenuManager?.HideContextMenu(_contextMenu);
        }
    }
    
    private ContextMenu? _contextMenu;
}