using EasyGameFramework.Api.Rendering;
using EasyGameFramework.Core;
using EasyGameFramework.Glfw;
using EasyGameFramework.OpenGL;

namespace EasyGameFramework.Api;

internal class NullRenderer : IRenderer
{
}

public sealed class EngineBuilder
{
    private bool m_IsRenderingApiSet;
    private bool m_IsLoggerSet;
    private bool m_IsRendererSet;
    
    private DiContainer DiContainer { get; } = new();
    
    public EngineBuilder WithOpenGl()
    {
        DiContainer.Register<IGpu, Gpu_GL>();
        m_IsRenderingApiSet = true;
        return this;
    }

    public EngineBuilder WithRenderer<TRenderer>() where TRenderer : IRenderer
    {
        DiContainer.Register<IRenderer, TRenderer>();
        m_IsRendererSet = true;
        return this;
    }

    public EngineBuilder WithLogger<TLogger>() where TLogger : ILogger
    {
        DiContainer.Register<ILogger, TLogger>();
        m_IsLoggerSet = true;
        return this;
    }

    public EngineBuilder WithApp<TApp>() where TApp : IApp
    {
        DiContainer.Register<IApp, TApp>();
        return this;
    }

    public IEngine Build()
    {
        if (!m_IsRenderingApiSet)
            WithOpenGl();

        if (!m_IsRendererSet)
            WithRenderer<NullRenderer>();

        if (!m_IsLoggerSet)
            WithLogger<ConsoleLogger>();
        
        RegisterWindowingSystem();
        
        DiContainer.Register<IInput, Input>();
        DiContainer.Register<IEngine, Engine>();
        DiContainer.Register<IContext, Context>();
        DiContainer.Register<IAllocator>(() => DiContainer);

        var engine = DiContainer.GetInstance<IEngine>();
        return engine;
    }

    private void RegisterWindowingSystem()
    {
        DiContainer.Register<IDisplays, Displays_GLFW>();
        DiContainer.Register<IWindow, Window_GLFW>();
    }
}