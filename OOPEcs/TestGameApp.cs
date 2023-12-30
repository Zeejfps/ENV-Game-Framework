using EasyGameFramework.Api;
using Entities;
using OpenGLSandbox;
using Tetris;

namespace OOPEcs;

public sealed class TestGameApp : WindowedApp
{
    private Context Context { get; } = new();
    
    public TestGameApp(IWindow window, ILogger logger) : base(window, logger)
    {
        Context.RegisterSingleton<IWindow>(window);
        Context.RegisterSingleton<ILogger>(logger);
        Context.RegisterSingletonEntity<ITextRenderer, Renderer>();
        Context.RegisterSingletonEntity<ISpriteRenderer, Renderer>();
        Context.RegisterSingletonEntity<IClock, GameClock>();
        //Context.RegisterTransientEntity<HelloWorldEntity>();
        //Context.RegisterTransientEntity<Monomino>();
        //Context.RegisterTransientEntity<Tetromino>();
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