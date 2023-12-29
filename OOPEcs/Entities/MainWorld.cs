using EasyGameFramework.Api;
using OpenGLSandbox;

namespace Tetris;

public sealed class MainWorld : IEntity
{
    private Context Context { get; }
    
    public MainWorld(
        Context parentContext,
        IWindow window
    )
    {
        Context = new Context(parentContext);
        Context.RegisterSingleton(window.Input.Keyboard);
        Context.RegisterTransientEntity<HelloWorldEntity>();
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