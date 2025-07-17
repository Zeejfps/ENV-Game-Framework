namespace ZGF.Gui.Tests;

public sealed class AppBar : Component
{
    public AppBar()
    {
        var container = new Rect
        {
            Style =
            {
                BackgroundColor = 0x000000,
                Padding = new PaddingStyle
                {
                    Bottom = 1,
                }
            }
        };
        var background = new Rect
        {
            Style = 
            {
                BackgroundColor = 0xDEDEDE,
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
        var label = new Label("Test");
        background.Add(label);
        container.Add(background);
        Add(container);
    }
}