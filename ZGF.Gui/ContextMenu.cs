using ZGF.Geometry;
using ZGF.Gui.Layouts;

namespace ZGF.Gui.Tests;

public sealed class ContextMenu : MultiChildView
{
    public override IComponentCollection Children => _itemsContainer.Children;

    private readonly ColumnView _itemsContainer;
    private readonly RectView _background;

    public uint BackgroundColor
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
        };

        AddChildToSelf(_background);
    }

    protected override void OnLayoutSelf()
    {
        AlignShortcutColumn();
        var width = MeasureWidth();
        var height = MeasureHeight(width);
        Position = new RectF { Left = 0, Bottom = 0, Width = width, Height = height };
    }

    // Gap reserved between the label column and the shortcut column so long labels don't
    // crowd their shortcut hints.
    private const float ShortcutColumnGap = 32f;

    // Gives every item that carries a shortcut the same label-cell width — the widest label
    // among them plus a fixed gap — so their shortcut hints line up in a single column.
    private void AlignShortcutColumn()
    {
        var maxLabel = 0f;
        foreach (var child in Children)
            if (child is ContextMenuItem { HasShortcut: true } item)
            {
                var w = item.MeasureLabelWidth();
                if (w > maxLabel) maxLabel = w;
            }
        if (maxLabel <= 0f) return;
        var columnWidth = maxLabel + ShortcutColumnGap;
        foreach (var child in Children)
            if (child is ContextMenuItem { HasShortcut: true } item)
                item.SetLabelColumnWidth(columnWidth);
    }
}
