using ZGF.Geometry;
using ZGF.Gui.Layouts;

namespace ZGF.Gui.Tests;

public sealed class AppBar : Component
{
    public AppBar()
    {
        var container = new Panel
        {
            BackgroundColor = 0x000000,
            Style =
            {
                Padding = new PaddingStyle
                {
                    Bottom = 1,
                }
            }
        };
        var background = new Panel
        {
            BackgroundColor = 0xDEDEDE,
            Style = 
            {
                BorderSize = BorderSizeStyle.All(1),
                BorderColor = new BorderColorStyle
                {
                    Top = 0xFFFFFF,
                    Left = 0xFFFFFF,
                    Right = 0x9C9C9C,
                    Bottom = 0x9C9C9C
                }
            }
        };

        var fileLabel = new Label("File")
        {
            Constraints = new RectF
            {
                Width = 20f
            }
        };
        var editLabel = new Label("Edit");
        var viewLabel = new Label("View");
        var specialLabel = new Label("Special");
        var helpLabel = new Label("Help");
        
        var row = new FlexRow(MainAxisAlignment.Start, CrossAxisAlignment.Stretch, 10)
        {
            fileLabel,
            editLabel,
            viewLabel,
            specialLabel,
            helpLabel,
        };
        
        var label = new Label("File    Edit    View    Special    Help")
        {
            VerticalTextAlignment = TextAlignment.Center,
        };
        background.Add(row);
        container.Add(background);
        Add(container);
    }
}