using ZGF.Gui.Layouts;

namespace ZGF.Gui.Tests;

public sealed class ContextMenuItemData
{
    public string Text { get; set; }
}

public sealed class ContextMenuItem : Component
{
    public List<ContextMenuItemData> SubOptions { get; } = new();

    private readonly ContextMenu _contextMenu;
    private readonly Panel _bg;

    private ContextMenuManager? ContextMenuManager => Get<ContextMenuManager>();

    public ContextMenuItem(ContextMenu contextMenu, string name)
    {
        ZIndex = 2;

        _contextMenu = contextMenu;
        _bg = new Panel
        {
            BackgroundColor = 0xDEDEDE,
            Padding = PaddingStyle.All(4)
        };

        var row = new Row
        {
            new Label(name),
            new Image
            {
                ImageUri = "Assets/Icons/arrow_right.png",
            }
        };
        row.Gap = 5;

        if (SubOptions.Count > 0)
        {
            row.Add(new Label("O"));
        }

        _bg.Add(row);
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

    private ContextMenu? _subMenu;

    protected override void OnMouseEnter()
    {
        _bg.BackgroundColor = 0x9C9CCE;
        ContextMenuManager?.SetKeepOpen(_contextMenu);
        if (SubOptions.Count > 0)
        {
            _subMenu = ContextMenuManager?.ShowContextMenu(Position.TopRight, _contextMenu);
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