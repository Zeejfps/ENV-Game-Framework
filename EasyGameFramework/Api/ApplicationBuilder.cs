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
        DiContainer.Bind<IGpu, Gpu_GL>();
        m_IsRenderingApiSet = true;
        return this;
    }

    public ApplicationBuilder WithRenderer<TRenderer>() where TRenderer : IRenderer
    {
        DiContainer.Bind<IRenderer, TRenderer>();
        m_IsRendererSet = true;
        return this;
    }

    public ApplicationBuilder WithLogger<TLogger>() where TLogger : ILogger
    {
        DiContainer.Bind<ILogger, TLogger>();
        m_IsLoggerSet = true;
        return this;
    }

    public ApplicationBuilder With<T, TImpl>() where TImpl : T
    {
        DiContainer.Bind<T, TImpl>();
        return this;
    }

    public IApp Build<TApp>() where TApp : IApp
    {
        if (!m_IsRenderingApiSet)
            WithOpenGl();

        if (!m_IsRendererSet)
            WithRenderer<NullRenderer>();

        if (!m_IsLoggerSet)
            WithLogger<ConsoleLogger>();
        
        RegisterWindowingSystem();
        
        DiContainer.Bind<IMouse, Mouse>();
        DiContainer.Bind<IKeyboard, Keyboard>();
        DiContainer.Bind<IInput, Input>();
        DiContainer.Bind<IContext, Context>();
        DiContainer.Bind<IEventBus, EventBus>();
        DiContainer.Bind<IApp, TApp>();
        DiContainer.Bind<IPlayerPrefs, IniPlayerPrefs>();
        DiContainer.BindInstance<IContainer>(DiContainer);

        var engine = DiContainer.New<IApp>();
        return engine;
    }

    private void RegisterWindowingSystem()
    {
        DiContainer.Bind<IDisplays, Displays_GLFW>();
        DiContainer.Bind<IWindow, Window_GLFW>();
    }
}

// TODO: Probably delete
internal class NullRenderer : IRenderer
{
}