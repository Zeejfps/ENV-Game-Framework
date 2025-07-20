using ZGF.Geometry;

namespace ZGF.Gui.Tests;

public interface IMenuItemController : IDisposable
{
    void OnMouseEnter();
    void OnMouseExit();
}

public interface IMenuItem
{
    string? Text { get; set; }
    bool IsDisabled { get; set; }
    bool IsHovered { get; set; }
    RectF Position { get; }
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

    private bool _isHovered;

    public bool IsHovered
    {
        get => _isHovered;
        set
        {
            if (SetField(ref _isHovered, value))
            {
                if (_isHovered)
                {
                    _background.BackgroundColor = 0x9C9CCE;
                }
                else
                {
                    _background.BackgroundColor = 0xDEDEDE;
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

        IsInteractable = true;

        _background.Add(_label);
        Add(_background);
    }

    protected override void OnAttachedToContext(Context context)
    {
        base.OnAttachedToContext(context);
        Controller = _controllerFactory.Invoke(this);
        ContextMenuManager = context.Get<ContextMenuManager>();
    }

    protected override void OnDetachedFromContext(Context context)
    {
        ContextMenuManager = null;
        Controller?.Dispose();
        Controller = null;
        base.OnDetachedFromContext(context);
    }

    protected override void OnMouseEnter()
    {
        Controller?.OnMouseEnter();
    }

    protected override void OnMouseExit()
    {
        Controller?.OnMouseExit();
    }
}