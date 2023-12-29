using EasyGameFramework.Api;
using OpenGLSandbox;

namespace Tetris;

public sealed class MainWorld : IEntity
{
    private Context Context { get; } = new();
    
    public MainWorld(
        Context context,
        IWindow window,
        IClock clock,
        ITextRenderer textRenderer,
        ILogger logger
    )
    {
        //Context = new World(parentContext);
        Context.RegisterSingleton(window);
        Context.RegisterSingleton<ILogger>(logger);
        Context.RegisterSingleton(window.Input.Keyboard);
        Context.RegisterSingleton<ITextRenderer>(textRenderer);
        Context.RegisterSingleton<IClock>(clock);    
        Context.RegisterTransientEntity<SuperTestEntity>();
        Context.RegisterTransientEntity<QuitGameInputAction>();
    }

    public void Load()
    {
        Context.Load();
    }

    public void Unload()
    {
        Context.Unload();
    }
}