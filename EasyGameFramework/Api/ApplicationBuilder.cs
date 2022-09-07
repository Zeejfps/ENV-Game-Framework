using EasyGameFramework.Api.InputDevices;
using EasyGameFramework.Api.Rendering;
using EasyGameFramework.Core;
using EasyGameFramework.Glfw;
using EasyGameFramework.OpenGL;

namespace EasyGameFramework.Api;

/// <summary>
/// Use this class to start building your GPU powered application.
/// </summary>
public sealed class ApplicationBuilder
{
    private bool m_IsRenderingApiSet;
    private bool m_IsLoggerSet;
    private bool m_IsRendererSet;
    
    private DiContainer DiContainer { get; } = new();
    
    public ApplicationBuilder WithOpenGl()
    {
        DiContainer.BindSingleton<IGpu, Gpu_GL>();
        m_IsRenderingApiSet = true;
        return this;
    }

    public ApplicationBuilder WithRenderer<TRenderer>() where TRenderer : IRenderer
    {
        DiContainer.BindSingleton<IRenderer, TRenderer>();
        m_IsRendererSet = true;
        return this;
    }

    public ApplicationBuilder WithLogger<TLogger>() where TLogger : ILogger
    {
        DiContainer.BindSingleton<ILogger, TLogger>();
        m_IsLoggerSet = true;
        return this;
    }

    public ApplicationBuilder With<T, TImpl>() where TImpl : T
    {
        DiContainer.BindSingleton<T, TImpl>();
        return this;
    }

    public TApp Build<TApp>() where TApp : WindowedApp
    {
        if (!m_IsRenderingApiSet)
            WithOpenGl();

        if (!m_IsRendererSet)
            WithRenderer<NullRenderer>();

        if (!m_IsLoggerSet)
            WithLogger<ConsoleLogger>();
        
        RegisterWindowingSystem();
        
        DiContainer.BindSingleton<IMouse, Mouse>();
        DiContainer.BindSingleton<IEventLoop, EventLoop>();
        DiContainer.BindSingleton<IKeyboard, Keyboard>();
        DiContainer.BindSingleton<IInputSystem, InputSystem>();
        DiContainer.BindSingleton<IContext, Context>();
        DiContainer.BindSingleton<IEventBus, EventBus>();
        DiContainer.BindSingleton<IPlayerPrefs, IniPlayerPrefs>();
        DiContainer.BindSingleton<IContainer>(DiContainer);
        DiContainer.BindSingleton<IGamepadManager, GamepadManager>();

        var engine = DiContainer.New<TApp>();
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