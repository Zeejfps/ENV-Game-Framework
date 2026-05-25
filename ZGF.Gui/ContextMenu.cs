using ZGF.Geometry;
using ZGF.Gui.Layouts;

namespace ZGF.Gui.Tests;

public sealed class ContextMenu : MultiChildView
{
    public override IComponentCollection Children => _itemsContainer.Children;

    private readonly ColumnView _itemsContainer;
    private readonly RectView _background;

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
        _itemsContainer = new ColumnView { Gap = 4 };

        _background = new RectView
        {
            BackgroundColor = 0xFFDEDEDE,
            Padding = PaddingStyle.All(4),
            BorderSize = new BorderSizeStyle { Left = 1, Right = 1, Bottom = 1 },
            BoxShadow = new BoxShadowStyle
            {
                OffsetX = 0f,
                OffsetY = -4f,
                Blur = 16f,
                Spread = 0f,
                Color = 0x60000000,
            },
            Children = { _itemsContainer },
            StyleClasses = { "raised_panel" }
        };

        AddChildToSelf(_background);
    }

    protected override void OnLayoutSelf()
    {
        var width = MeasureWidth();
        var height = MeasureHeight(width);
        Position = new RectF { Left = 0, Bottom = 0, Width = width, Height = height };
    }
}
