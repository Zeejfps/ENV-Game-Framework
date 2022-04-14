using Framework;

namespace Framework;

public class Game
{
    IDisplays Displays => Context.Displays;
    IWindow Window => Context.Window;
    IContext Context { get; }

    public Game(IContext context)
    {
        Context = context;
    }
    
    public void Run()
    {
        var primaryDisplay = Displays.PrimaryDisplay;
        
        Window.Title = "Hello World";
        //Window.IsFullscreen = true;
        Window.Width = 1280;
        Window.Height = 720;
        Window.PosX = (int)((primaryDisplay.ResolutionX - Window.Width) * 0.5f);
        Window.PosY = (int)((primaryDisplay.ResolutionY - Window.Height) * 0.5f);
        Window.IsResizable = true;
        Window.IsVsyncEnabled = false;

        Window.Open();
        
        var scene = new TestScene(Context);
        scene.Load();
        
        while (Window.IsOpened)
        {
            Window.Update();
            scene.Update();
        }
    }
}