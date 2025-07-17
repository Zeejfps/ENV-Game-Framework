using ZGF.Geometry;
using ZGF.Gui.Layouts;

namespace ZGF.Gui.Tests;

public sealed class ContextMenu : Component
{
    public ContextMenu()
    {
        var option1 = new Label("Option 1");
        var option2 = new Label("Option 1");
        var option3 = new Label("Option 1");
        var option4 = new Label("Option 1");

        var column = new Column
        {
            option1,
            option2,
            option3,
            option4,
        };

        ZIndex = 10;
        
        Add(column);
    }

    protected override void OnLayoutSelf()
    {
        var width = MeasureWidth();
        var height = MeasureHeight();
        var bottom = BottomConstraint - height;

        Position = new RectF
        {
            Left = LeftConstraint,
            Bottom = bottom,
            Width = width,
            Height = height,
        };
    }
}