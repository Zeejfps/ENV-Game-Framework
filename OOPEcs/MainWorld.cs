using EasyGameFramework.Api;
using EasyGameFramework.Api.Events;
using EasyGameFramework.Api.InputDevices;
using OpenGLSandbox;

namespace Tetris;

public sealed class MainWorld : World
{
    public MainWorld(
        IWindow window,
        ITextRenderer textRenderer
    ) {
        RegisterSingleton<ITextRenderer>(textRenderer);
        RegisterSingleton(window);
        RegisterSingleton(window.Input.Keyboard);
        RegisterSingleton<IClock, Clock>();    
        RegisterEntity<SuperTestEntity>();
        RegisterEntity<QuitGameInputAction>();
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