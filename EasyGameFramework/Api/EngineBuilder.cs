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
    private bool m_IsBackendSet;
    private bool m_IsLoggerSet;
    private bool m_IsRendererSet;
    
    private DiContainer DiContainer { get; } = new();
    
    public EngineBuilder WithOpenGl()
    {
        DiContainer.Register<IGpu, Gpu_GL>();
        m_IsBackendSet = true;
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

    public EngineBuilder WithDefaultRenderer()
    {
        DiContainer.Register<IRenderer, NullRenderer>();
        return this;
    }

    public EngineBuilder WithGame<TGame>() where TGame : IApp
    {
        DiContainer.Register<IApp, TGame>();
        return this;
    }

    public IEngine Build()
    {
        if (!m_IsBackendSet)
            WithOpenGl();

        if (!m_IsRendererSet)
            WithDefaultRenderer();

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