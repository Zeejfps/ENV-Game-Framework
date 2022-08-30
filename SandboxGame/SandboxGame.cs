using EasyGameFramework;
using EasyGameFramework.API;

namespace Framework;

public class SandboxGame : Game
{
    IDisplays Displays => Context.Displays;
    IWindow Window => Context.Window;
    IContext Context { get; }

    private TestScene Scene { get; set; }
    
    public SandboxGame(IContext context)
    {
        Context = context;
    }

    protected override void OnStart()
    {
        Window.Title = "Hello World";
        //Window.IsFullscreen = true;
        Window.Width = 1280;
        Window.Height = 720;
        Window.IsResizable = true;
        Window.IsVsyncEnabled = true;
        Window.ShowCentered();
        
        Scene = new TestScene(Context);
        Scene.Load();
    }

    protected override void OnUpdate(float dt)
    {
        Scene.Update(dt);
    }

    protected override void OnRender(float dt)
    {
        Scene.Render();
    }

    protected override void OnQuit()
    {
        
    }
}