using EasyGameFramework.Api;

namespace Framework;

public class SandboxGame : Game
{
    private TestScene Scene { get; }
    
    public SandboxGame(IGameContext context) : base(context)
    {
        Scene = new TestScene(this, Logger);
    }
    
    protected override void OnStartup()
    {
        var window = Window;
        window.Title = "Sandbox Game";
        //Window.IsFullscreen = true;
        window.ScreenWidth = 1280;
        window.ScreenHeight = 720;
        window.IsResizable = true;
        window.IsVsyncEnabled = true;
        
        Scene.Load();
    }

    protected override void OnFixedUpdate()
    {
        Scene.Update(Time.FixedUpdateDeltaTime);
    }

    protected override void OnUpdate()
    {
        Scene.Render();
    }

    protected override void OnShutdown()
    {
        
    }
}