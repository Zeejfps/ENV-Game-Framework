﻿using EasyGameFramework.API;
using Framework.GLFW.NET;
using GlfwOpenGLBackend;

namespace EasyGameFramework;

public sealed class EngineBuilder
{
    private DiContainer DiContainer { get; } = new();

    private bool m_IsWindowingSystemSet;
    private bool m_IsBackendSet;
    private bool m_IsRendererSet;

    public EngineBuilder WithGlfw()
    {
        DiContainer.Register<IDisplays, Displays_GLFW>();
        DiContainer.Register<IInput, Input_GLFW>();
        DiContainer.Register<IWindow, Window_GLFW>();
        m_IsWindowingSystemSet = true;
        return this;
    }
    
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
    
    public EngineBuilder WithDefaultRenderer()
    {
        return this;
    }

    public EngineBuilder WithGame<TGame>() where TGame : IGame
    {
        DiContainer.Register<IGame, TGame>();
        return this;
    }
    
    public IEngine Build()
    {
        if (!m_IsWindowingSystemSet)
            WithGlfw();
        
        if (!m_IsBackendSet)
            WithOpenGl();

        if (!m_IsRendererSet)
            WithDefaultRenderer();
        
        DiContainer.Register<IEngine, Engine>();
        DiContainer.Register<IContext, Context>();
        
        var engine = DiContainer.GetInstance<IEngine>();
        return engine;
    }
}

internal class DiContainer
{
    private readonly Dictionary<Type, object> m_TypeToInstanceMap = new();
    private readonly Dictionary<Type, Func<object>> m_TypeToFactoryMap = new();

    public T GetInstance<T>()
    {
        return (T)GetInstance(typeof(T));
    }

    private object GetInstance(Type type)
    {
        if (m_TypeToInstanceMap.TryGetValue(type, out var instance))
            return instance;

        if (m_TypeToFactoryMap.TryGetValue(type, out Func<object> factory))
        {
            instance = factory.Invoke();
            m_TypeToInstanceMap[type] = instance;
            return instance;
        }

        if (!type.IsAbstract)
        {
            instance = CreateInstance(type);
            m_TypeToInstanceMap[type] = instance;
            return instance;
        }
        
        throw new InvalidOperationException("No registration for " + type);
    }

    private object CreateInstance(Type implementationType)
    {
        var ctor = implementationType.GetConstructors().Single();
        var paramTypes = ctor.GetParameters().Select(p => p.ParameterType);
        var dependencies = paramTypes.Select(GetInstance).ToArray();
        var obj = Activator.CreateInstance(implementationType, dependencies);
        if (obj == null)
            throw new Exception($"Failed to get instance of obj: {implementationType}");
        return obj;
    }

    public void Register<T, TImpl>()
    {
        m_TypeToFactoryMap.Add(typeof(T), () => CreateInstance(typeof(TImpl)));
    }
}