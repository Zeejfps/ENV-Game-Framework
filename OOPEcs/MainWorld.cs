using EasyGameFramework.Api;
using EasyGameFramework.Api.Events;
using EasyGameFramework.Api.InputDevices;
using OpenGLSandbox;

namespace Tetris;

public sealed class MainWorld : IEntity
{
    private World Context { get; } = new();
    
    public MainWorld(
        IWindow window,
        IClock clock,
        ITextRenderer textRenderer
    )
    {
        //Context = new World(parentContext);
        Context.RegisterSingleton(window);
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
    private readonly ITextRenderer m_TextRenderer;

    public QuitGameInputAction(IWindow window, ITextRenderer textRenderer)
    {
        m_Window = window;
        m_TextRenderer = textRenderer;
    }

    public void Load()
    {
        m_Window.Input.Keyboard.KeyPressed += Keyboard_OnKeyPressed;
        m_TextRenderer.Render("Hello World!", new Rect(0, 0, 100, 100), new TextStyle
        {
            FontName = "test",
            Color = Color.FromHex(0xff00ff, 1f),
        });
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