using Framework.GLFW.NET;
using GlfwOpenGLBackend;

namespace EasyGameFramework.API;

public sealed class ApplicationBuilder
{
    private DiContainer DiContainer { get; } = new();
    
    private bool m_IsBackendSet;
    private bool m_IsRendererSet;
    
    public ApplicationBuilder WithGlfwOpenGlBackend()
    {
        DiContainer.Register<IDisplays, Displays_GLFW>();
        DiContainer.Register<IInput, Input_GLFW>();
        DiContainer.Register<IWindow, Window_GLFW>();
        DiContainer.Register<IGpu, Gpu_GL>();
        DiContainer.Register<IApplication, Application_GLFW_GL>();
        m_IsBackendSet = true;
        return this;
    }
    
    public ApplicationBuilder WithRenderer<TRenderer>() where TRenderer : IRenderer
    {
        DiContainer.Register<IRenderer, TRenderer>();
        m_IsRendererSet = true;
        return this;
    }
    
    public ApplicationBuilder WithDefaultRenderer()
    {
        return this;
    }
    
    public IApplication Build()
    {
        if (!m_IsBackendSet)
            WithGlfwOpenGlBackend();

        if (!m_IsRendererSet)
            WithDefaultRenderer();
        
        var app = DiContainer.GetInstance<IApplication>();
        return app;
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