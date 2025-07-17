namespace ZGF.Gui.Tests;

public sealed class Center : Component
{
    public Window Window { get; }
    
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

        Add(new Window("About This Computer"));

        Window = new Window("Window Title Here");
        
        Add(Window);
        
    }
}