using ZGF.Geometry;
using ZGF.Gui.Layouts;

namespace ZGF.Gui.Tests;

public sealed class ContextMenu : View
{
    private readonly ColumnView _itemsContainer;

    public override IComponentCollection Children => _itemsContainer.Children;

    private PointF _anchorPoint;
    public PointF AnchorPoint
    {
        get => _anchorPoint;
        set => SetField(ref _anchorPoint, value);
    }

    public ContextMenu()
    {
        _itemsContainer = new ColumnView
        {
            Gap = 4
        };
        ZIndex = 1;
        
        var background = new RectView
        {
            BackgroundColor = 0xFFDEDEDE,
            Padding = PaddingStyle.All(4),
            BorderSize = new BorderSizeStyle
            {
                Left = 1,
                Right = 1,
                Bottom = 1
            },
            Children =
            {
                _itemsContainer
            },
            StyleClasses =
            {
                "raised_panel"
            }
        };
        
        AddChildToSelf(background);
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
}