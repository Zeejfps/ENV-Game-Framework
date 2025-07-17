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
            VerticalTextAlignment = TextAlignment.Center,
        };
        var editLabel = new Label("Edit")
        {
            VerticalTextAlignment = TextAlignment.Center,
        };
        var viewLabel = new Label("View")
        {
            VerticalTextAlignment = TextAlignment.Center,
        };
        var specialLabel = new Label("Special")
        {
            VerticalTextAlignment = TextAlignment.Center,
        };
        var helpLabel = new Label("Help")
        {
            VerticalTextAlignment = TextAlignment.Center,
        };
        
        var row = new FlexRow(MainAxisAlignment.Start, CrossAxisAlignment.Stretch, 10)
        {
            fileLabel,
            editLabel,
            viewLabel,
            specialLabel,
            helpLabel,
        };
        
        background.Add(row);
        container.Add(background);
        Add(container);
    }
}