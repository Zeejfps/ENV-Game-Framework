using EasyGameFramework.Api;

namespace Framework;

public class SandboxGame : Game
{
    IDisplays Displays => Context.Displays;
    IContext Context { get; }

    private TestScene Scene { get; set; }
    
    public SandboxGame(IContext context) : base(context.Window, context.Input)
    {
        Context = context;
    }

    protected override void OnSetup()
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

    protected override void OnUpdate()
    {
        Scene.Update(Clock.DeltaTime);
    }

    protected override void OnRender()
    {
        Scene.Render();
    }

    protected override void OnTeardown()
    {
        
    }
}