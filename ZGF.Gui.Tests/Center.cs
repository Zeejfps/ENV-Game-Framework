namespace ZGF.Gui.Tests;

public sealed class Center : View
{
    public Window Window { get; }
    
    public Center()
    {
        var background = new RectView
        {
            BackgroundColor = 0x9C9CCE,
        };
        
        AddChildToSelf(background);

        var w = new Window("About This Computer");
        w.Controller = new WindowDefaultKbmController(w);
        AddChildToSelf(w);

        Window = new Window("Window Title Here");
        Window.Controller = new WindowDefaultKbmController(Window);
        AddChildToSelf(Window);
        
    }
}