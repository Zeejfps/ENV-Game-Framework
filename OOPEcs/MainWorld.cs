using EasyGameFramework.Api;
using EasyGameFramework.Api.Events;
using EasyGameFramework.Api.InputDevices;
using OpenGLSandbox;

namespace Tetris;

public sealed class MainWorld : IEntity
{
    private EntityContext Context { get; } = new();
    
    public MainWorld(
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

public sealed class QuitGameInputAction : IEntity
{
    private readonly IWindow m_Window;
    private readonly ILogger m_Logger;


    public QuitGameInputAction(IWindow window, ILogger logger)
    {
        m_Window = window;
        m_Logger = logger;
    }

    public void Load()
    {
        m_Window.Input.Keyboard.KeyPressed += Keyboard_OnKeyPressed;
    }

    public void Unload()
    {
        m_Window.Input.Keyboard.KeyPressed -= Keyboard_OnKeyPressed;
    }
    
    private void Keyboard_OnKeyPressed(in KeyboardKeyStateChangedEvent evt)
    {
        if (evt.Key == KeyboardKey.Escape)
        {
            m_Window.Close();
        }
    }
}