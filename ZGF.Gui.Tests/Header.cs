namespace ZGF.Gui.Tests;

public sealed class Header : Component
{
    public Header()
    {
        var background = new Rect
        {
            Style = 
            {
                BackgroundColor = 0xDEDEDE,
                BorderColor = new BorderColorStyle
                {
                    Top = 0xFFFFFF,
                    Left = 0xFFFFFF,
                    Right = 0x9C9C9C,
                    Bottom = 0x9C9C9C
                }
            }
        };
        
        Add(background);
    }
}