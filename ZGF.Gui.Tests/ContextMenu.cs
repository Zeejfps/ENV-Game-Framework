using ZGF.Geometry;
using ZGF.Gui.Layouts;

namespace ZGF.Gui.Tests;

public sealed class ContextMenu : Component
{
    private readonly PointF _anchorPoint;
    private readonly ContextMenu? _parentMenu;
    private readonly Column _itemsContainer;

    public ContextMenu? ParentMenu => _parentMenu;

    private ContextMenuManager? ContextMenuManager => Get<ContextMenuManager>();

    public ContextMenu(PointF anchorPoint, ContextMenu? parentMenu = null)
    {
        _anchorPoint = anchorPoint;
        _parentMenu = parentMenu;

        var background = new Panel
        {
            BackgroundColor = 0xDEDEDE,
            Padding = PaddingStyle.All(4),
            BorderSize = new BorderSizeStyle
            {
                Left = 1,
                Right = 1,
                Bottom = 1
            }
        };
        background.AddStyleClass("raised_panel");

        IsInteractable = true;

        _itemsContainer = new Column
        {
            Gap = 4
        };
        ZIndex = 1;
        
        background.Add(_itemsContainer);
        Add(background);
    }

    public void AddItem(ContextMenuItem item)
    {
        _itemsContainer.Add(item);
    }

    protected override void OnLayoutSelf()
    {
        var width = MeasureWidth();
        var height = MeasureHeight();
        var bottom = _anchorPoint.Y - height;

        Position = new RectF
        {
            Left = _anchorPoint.X,
            Bottom = bottom,
            Width = width,
            Height = height,
        };
    }
    
    protected override void OnMouseEnter()
    {
        ContextMenuManager?.SetKeepOpen(this);
    }

    protected override void OnMouseExit()
    {
        ContextMenuManager?.HideContextMenu(this);
    }
}