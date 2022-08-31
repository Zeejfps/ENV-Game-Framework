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
        DiContainer.Register<IGpu, Gpu_GL>();
        m_IsRenderingApiSet = true;
        return this;
    }

    public ApplicationBuilder WithRenderer<TRenderer>() where TRenderer : IRenderer
    {
        DiContainer.Register<IRenderer, TRenderer>();
        m_IsRendererSet = true;
        return this;
    }

    public ApplicationBuilder WithLogger<TLogger>() where TLogger : ILogger
    {
        DiContainer.Register<ILogger, TLogger>();
        m_IsLoggerSet = true;
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
        
        DiContainer.Register<IInput, Input>();
        DiContainer.Register<IContext, Context>();
        DiContainer.Register<IAllocator>(() => DiContainer);
        DiContainer.Register<IApp, TApp>();

        var engine = DiContainer.GetInstance<IApp>();
        return engine;
    }

    private void RegisterWindowingSystem()
    {
        DiContainer.Register<IDisplays, Displays_GLFW>();
        DiContainer.Register<IWindow, Window_GLFW>();
    }
}

// TODO: Probably delete
internal class NullRenderer : IRenderer
{
}