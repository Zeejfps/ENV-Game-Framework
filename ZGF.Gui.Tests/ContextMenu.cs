using ZGF.Geometry;
using ZGF.Gui.Layouts;

namespace ZGF.Gui.Tests;

public sealed class ContextMenu : Component
{
    private readonly PointF _anchorPoint;
    private readonly ContextMenu? _parentMenu;

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

        var option1 = new ContextMenuItem(this, "Option 1");
        var option2 = new ContextMenuItem(this, "Option 2");
        var option3 = new ContextMenuItem(this, "Option 3")
        {
            SubOptions =
            {
                new ContextMenuItemData
                {
                    Text = "Test1"
                },
                new ContextMenuItemData
                {
                    Text = "Test2"
                },
                new ContextMenuItemData
                {
                    Text = "Test3"
                },
            }
        };
        var option4 = new ContextMenuItem(this, "Option 4");

        var column = new Column
        {
            option1,
            option2,
            option3,
            option4,
        };
        column.Gap = 4;
        
        ZIndex = 1;
        
        background.Add(column);
        Add(background);
    }

    public void AddItem()
    {

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
        ContextMenuManager?.SetKeepOpen(this);
    }

    protected override void OnMouseExit()
    {
        ContextMenuManager?.HideContextMenu(this);
    }
}