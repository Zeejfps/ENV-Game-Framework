﻿using ZGF.Geometry;
using ZGF.Gui.Layouts;

namespace ZGF.Gui.Tests;

public sealed class ContextMenu : View
{
    private readonly ColumnView _itemsContainer;
    
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
            BackgroundColor = 0xDEDEDE,
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

    public void AddItem(ContextMenuItem item)
    {
        _itemsContainer.Children.Add(item);
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