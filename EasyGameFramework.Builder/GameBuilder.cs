using EasyGameFramework.Api;
using EasyGameFramework.Api.InputDevices;
using EasyGameFramework.Core;
using EasyGameFramework.Core.InputDevices;
using EasyGameFramework.Glfw;

namespace EasyGameFramework.Builder;

/// <summary>
/// Use this class to start building your GPU powered game.
/// </summary>
public sealed class GameBuilder
{
    private bool m_IsLoggerSet;
    private bool m_IsWindowSet;
    
    private DiContainer DiContainer { get; } = new();
    
    public GameBuilder WithWindow<TWindow>() where TWindow : IWindow
    {
        DiContainer.BindSingleton<IWindow, TWindow>();
        m_IsWindowSet = true;
        return this;
    }
 
    public GameBuilder WithLogger<TLogger>() where TLogger : ILogger
    {
        DiContainer.BindSingleton<ILogger, TLogger>();
        m_IsLoggerSet = true;
        return this;
    }

    public TGame Build<TGame>() where TGame : IGame
    {
        if (!m_IsWindowSet)
            WithWindow<Window_GLFW>();

        if (!m_IsLoggerSet)
            WithLogger<ConsoleLogger>();
        
        DiContainer.BindSingleton<IDisplayManager, DisplayManagerGlfw>();
        DiContainer.BindSingleton<IContext, Context>();
        DiContainer.BindSingleton<IMouse, Mouse>();
        DiContainer.BindSingleton<IKeyboard, Keyboard>();
        DiContainer.BindSingleton<IInputSystem, InputSystem>();
        DiContainer.BindSingleton<IEventBus, EventBus>();
        DiContainer.BindSingleton<IPlayerPrefs, IniPlayerPrefs>();
        DiContainer.BindSingleton<IGamepadManager, GamepadManager>();

        var engine = DiContainer.New<TGame>();
        return engine;
    }

    public void With<T, T1>() where T1 : T
    {
        DiContainer.BindSingleton<T, T1>();
    }
}