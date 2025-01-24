using OpenGLSandbox;

namespace EasyGameFramework.GUI;

public sealed class GridWidget : Widget
{
    public int RowCount { get; set; }
    public int ColumnCount { get; set; }
    public float Spacing { get; set; }
    public List<IWidget>? Children { get; set; } = new();
        
    protected override IWidget Build(IBuildContext context)
    {
        var availableWidth = ScreenRect.Width - (ColumnCount - 1) * Spacing;
        var availableHeight = ScreenRect.Height - (RowCount - 1) * Spacing;
        var cellWidth = availableWidth / ColumnCount;
        var cellHeight = availableHeight / RowCount;
        var xOffset = ScreenRect.X;
        var yOffset = ScreenRect.Y;
            
        for (var i = 0; i < RowCount; i++)
        {
            for (var j = 0; j < ColumnCount; j++)
            {
                var childIndex = j + i * ColumnCount;
                if (childIndex >= Children.Count)
                    break;

                Children[childIndex].ScreenRect = new Rect(j * cellWidth + xOffset + (j*Spacing), i * cellHeight + i * Spacing + yOffset, cellWidth, cellHeight);
            }
        }

        return new MultiChildWidget(Children);
    }
}