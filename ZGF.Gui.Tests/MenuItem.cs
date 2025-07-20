namespace ZGF.Gui.Tests;

public interface IMenuItemController : IDisposable
{

}

public interface IMenuItem
{
    string? Text { get; set; }
    bool IsDisabled { get; set; }
}

public sealed class MenuItem : Component, IMenuItem
{
    private readonly Panel _background;
    private readonly Label _label;
    private readonly Func<IMenuItem, IMenuItemController> _controllerFactory;

    private ContextMenuManager? ContextMenuManager { get; set; }
    private IMenuItemController? Controller { get; set; }

    public string? Text
    {
        get => _label.Text;
        set => _label.Text = value;
    }

    private bool _isDisabled;
    public bool IsDisabled
    {
        get => _isDisabled;
        set
        {
            if (SetField(ref _isDisabled, value))
            {
                if (_isDisabled)
                {
                    _label.AddStyleClass("disabled");
                }
                else
                {
                    _label.RemoveStyleClass("disabled");
                }
            }
        }
    }

    public MenuItem(Func<IMenuItem, IMenuItemController> controllerFactory)
    {
        _controllerFactory = controllerFactory;

        _background = new Panel
        {
            BackgroundColor = 0xDEDEDE,
            Padding = PaddingStyle.All(3)
        };
        
        _label = new Label
        {
            VerticalTextAlignment = TextAlignment.Center,
        };
        
        _background.Add(_label);
        Add(_background);
    }

    protected override void OnAttachedToContext(Context context)
    {
        base.OnAttachedToContext(context);
        Controller = _controllerFactory.Invoke(this);
        ContextMenuManager = context.Get<ContextMenuManager>();
        context.MouseInputSystem.EnableHover(this);
    }

    protected override void OnDetachedFromContext(Context context)
    {
        ContextMenuManager = null;
        Controller?.Dispose();
        Controller = null;
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