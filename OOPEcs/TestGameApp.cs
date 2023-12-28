using EasyGameFramework.Api;
using OpenGLSandbox;
using Tetris;

namespace OOPEcs;

public sealed class TestGameApp : WindowedApp
{
    private EntityContext Context { get; } = new();
    
    public TestGameApp(IWindow window, ILogger logger) : base(window, logger)
    {
        Context.RegisterSingleton<IWindow>(window);
        Context.RegisterSingleton<ILogger>(logger);
        Context.RegisterSingletonEntity<ITextRenderer, Renderer>();
        Context.RegisterSingletonEntity<IClock, GameClock>();
        Context.RegisterTransientEntity<QuitGameInputAction>();
        Context.RegisterTransientEntity<HelloWorldEntity>();
    }

    protected override void OnStartup()
    {
        Context.Load();
    }

    protected override void OnShutdown()
    {
        Context.Unload();
    }
}