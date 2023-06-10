using EasyGameFramework.Api;
using EasyGameFramework.Api.InputDevices;
using EasyGameFramework.Api.Rendering;
using EasyGameFramework.Core;
using EasyGameFramework.Core.InputDevices;
using EasyGameFramework.Glfw;

namespace EasyGameFramework.Builder;

/// <summary>
/// Use this class to start building your GPU powered application.
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
        
        DiContainer.BindSingleton<IDisplays, Displays_GLFW>();
        DiContainer.BindSingleton<IMouse, Mouse>();
        DiContainer.BindSingleton<IKeyboard, Keyboard>();
        DiContainer.BindSingleton<IInputSystem, InputSystem>();
        DiContainer.BindSingleton<IContext, Context>();
        DiContainer.BindSingleton<IEventBus, EventBus>();
        DiContainer.BindSingleton<IPlayerPrefs, IniPlayerPrefs>();
        DiContainer.BindSingleton<IContainer>(DiContainer);
        DiContainer.BindSingleton<IGamepadManager, GamepadManager>();

        var engine = DiContainer.New<TGame>();
        return engine;
    }
}