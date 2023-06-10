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
    private bool m_IsRendererSet;
    
    private DiContainer DiContainer { get; } = new();

    public GameBuilder WithRenderer<TRenderer>() where TRenderer : IRenderer
    {
        DiContainer.BindSingleton<IRenderer, TRenderer>();
        m_IsRendererSet = true;
        return this;
    }

    public GameBuilder WithLogger<TLogger>() where TLogger : ILogger
    {
        DiContainer.BindSingleton<ILogger, TLogger>();
        m_IsLoggerSet = true;
        return this;
    }

    public GameBuilder With<T, TImpl>() where TImpl : T
    {
        DiContainer.BindSingleton<T, TImpl>();
        return this;
    }

    public TGame Build<TGame>() where TGame : IGame
    {
        if (!m_IsRendererSet)
            WithRenderer<NullRenderer>();

        if (!m_IsLoggerSet)
            WithLogger<ConsoleLogger>();
        
        RegisterWindowingSystem();
        
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

    private void RegisterWindowingSystem()
    {
        DiContainer.BindSingleton<IDisplays, Displays_GLFW>();
        DiContainer.BindSingleton<IWindow, Window_GLFW>();
    }
}

// TODO: Probably delete
internal class NullRenderer : IRenderer
{
}