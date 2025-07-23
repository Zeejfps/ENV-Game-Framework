namespace ZGF.Gui.Tests;

public sealed class Center : Component
{
    public Window Window { get; }
    
    public Center()
    {
        var background = new Panel
        {
            BackgroundColor = 0x9C9CCE,
        };
        
        Add(background);

        var w = new Window("About This Computer");
        w.Controller = new WindowDefaultKbmController(w);
        Add(w);

        Window = new Window("Window Title Here");
        Window.Controller = new WindowDefaultKbmController(Window);
        Add(Window);
        
    }
}