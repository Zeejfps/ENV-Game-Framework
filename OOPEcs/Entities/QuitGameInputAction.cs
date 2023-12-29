using EasyGameFramework.Api;
using EasyGameFramework.Api.Events;
using EasyGameFramework.Api.InputDevices;

namespace Tetris;

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