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

        var option1 = new Label("Option 1");
        var option2 = new Label("Option 2");
        var option3 = new Label("Option 3");
        var option4 = new Label("Option 4");

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
        Console.WriteLine($"Measure: {width} {height}");
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