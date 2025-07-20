using ZGF.Gui.Layouts;

namespace ZGF.Gui.Tests;

public sealed class ContextMenuItemData
{
    public string Text { get; set; }
}

public interface IContextMenuItem
{

}

public interface IContextMenuItemController
{
    void Dispose();
}

public sealed class ContextMenuItem : Component
{
    public List<ContextMenuItemData> SubOptions { get; } = new();

    private readonly ContextMenu _contextMenu;
    private readonly Panel _bg;
    private readonly Image _arrowIcon;
    private readonly Label _label;

    private ContextMenuManager? ContextMenuManager => Get<ContextMenuManager>();
    private ContextMenu? _subMenu;

    public string? Text
    {
        get => _label.Text;
        set => _label.Text = value;
    }

    public ContextMenuItem(ContextMenu contextMenu, string name)
    {
        ZIndex = 2;

        _contextMenu = contextMenu;
        _bg = new Panel
        {
            BackgroundColor = 0xDEDEDE,
            Padding = PaddingStyle.All(4)
        };

        _arrowIcon = new Image
        {
            PreferredWidth = 20,
            PreferredHeight = 20
        };

        _label = new Label
        {
            Text = name,
            VerticalTextAlignment = TextAlignment.Center,
        };

        var row = new Row
        {
            _label,
            _arrowIcon,
        };
        row.Gap = 5;

        _bg.Add(row);
        Add(_bg);
    }

    protected override void OnAttachedToContext(Context context)
    {
        base.OnAttachedToContext(context);
        if (SubOptions.Count > 0)
        {
            _arrowIcon.ImageUri = "Assets/Icons/arrow_right.png";
        }
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
        if (SubOptions.Count > 0)
        {
            _subMenu = ContextMenuManager?.ShowContextMenu(Position.TopRight, _contextMenu);
            foreach (var subOption in SubOptions)
            {
                _subMenu.AddItem(new ContextMenuItem(_subMenu, subOption.Text));
            }
        }
    }

    protected override void OnMouseExit()
    {
        _bg.BackgroundColor = 0xDEDEDE;
        ContextMenuManager?.HideContextMenu(_contextMenu);

        if (_subMenu != null)
        {
            ContextMenuManager?.HideContextMenu(_subMenu);
        }
    }
}