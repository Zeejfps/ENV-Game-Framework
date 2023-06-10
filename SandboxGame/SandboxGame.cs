using EasyGameFramework.Api;

namespace Framework;

public class SandboxGame : Game
{
    private TestScene Scene { get; }
    
    public SandboxGame(IContext context) : base(context)
    {
        Scene = new TestScene(this, Logger);
    }

    protected override void Configure()
    {
        var window = Window;
        window.Title = "Sandbox Game";
        //Window.IsFullscreen = true;
        window.ScreenWidth = 1280;
        window.ScreenHeight = 720;
        window.IsResizable = true;
        window.IsVsyncEnabled = true;
    }

    protected override void OnStart()
    {
        Scene.Load();
    }

    protected override void OnUpdate()
    {
        Scene.Update(Time.UpdateDeltaTime);
    }

    protected override void OnRender()
    {
        Scene.Render();
    }

    protected override void OnStop()
    {
        
    }
}