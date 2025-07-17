using ZGF.Geometry;
using ZGF.Gui.Layouts;

namespace ZGF.Gui.Tests;

public sealed class ContextMenu : Component
{
    private readonly PointF _anchorPoint;

    public ContextMenu(PointF anchorPoint)
    {
        _anchorPoint = anchorPoint;

        var background = new Panel
        {
            BackgroundColor = 0xDEDEDE,
            Padding = PaddingStyle.All(4),
            BorderSize = new BorderSizeStyle
            {
                Left = 1,
                Right = 1,
                Bottom = 1
            },
            BorderColor = new BorderColorStyle
            {
                Top = 0xFFFFFF,
                Left = 0xFFFFFF,
                Right = 0x9C9C9C,
                Bottom = 0x9C9C9C
            }
        };

        var option1 = new ContextMenuItem("Option 1");
        var option2 = new ContextMenuItem("Option 2");
        var option3 = new ContextMenuItem("Option 3");
        var option4 = new ContextMenuItem("Option 4");

        var column = new Column
        {
            option1,
            option2,
            option3,
            option4,
        };
        column.Gap = 4;
        
        background.Add(column);
        Add(background);
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
        Get<ContextMenuManager>()?.SetKeepOpen(this);
    }

    protected override void OnMouseExit()
    {
        Get<ContextMenuManager>()?.HideContextMenu(this);
    }
}

public sealed class ContextMenuItem : Component
{
    private readonly Panel _bg;

    public ContextMenuItem(string name)
    {
        _bg = new Panel
        {
            BackgroundColor = 0xDEDEDE,
        };
        _bg.Add(new Label(name));
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
        Console.WriteLine("JEre?");
    }

    protected override void OnMouseExit()
    {
        _bg.BackgroundColor = 0xDEDEDE;
    }
}