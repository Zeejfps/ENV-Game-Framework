using ZGF.Geometry;
using ZGF.Gui.Layouts;

namespace ZGF.Gui.Tests;

public sealed class ContextMenu : View
{
    public override IComponentCollection Children => _itemsContainer.Children;

    private readonly ColumnView _itemsContainer;
    private readonly RectView _background;

    private PointF _anchorPoint;
    public PointF AnchorPoint
    {
        get => _anchorPoint;
        set => SetField(ref _anchorPoint, value);
    }

    public StyleValue<uint> BackgroundColor
    {
        get => _background.BackgroundColor;
        set => _background.BackgroundColor = value;
    }
    
    public StyleValue<PaddingStyle> Padding
    {
        get => _background.Padding;
        set => _background.Padding = value;
    }

    public StyleValue<BorderSizeStyle> BorderSize
    {
        get => _background.BorderSize;
        set => _background.BorderSize = value;
    }

    public StyleValue<BorderColorStyle> BorderColor
    {
        get => _background.BorderColor;
        set => _background.BorderColor = value;
    }
    
    public ContextMenu()
    {
        _itemsContainer = new ColumnView
        {
            Gap = 4
        };
        ZIndex = 1;
        
        _background = new RectView
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
        
        AddChildToSelf(_background);
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