namespace ZGF.Gui.Tests;

public sealed class Center : Component
{
    public Center()
    {
        var background = new Rect
        {
            Style =
            {
                BackgroundColor = 0x9C9CCE,
            }
        };
        
        Add(background);

        var window = new Window();
        
        Add(window);
    }
}