﻿namespace EasyGameFramework.Api;

internal class DiContainer : IAllocator
{
    private readonly Dictionary<Type, Func<object>> m_TypeToFactoryMap = new();
    private readonly Dictionary<Type, object> m_TypeToInstanceMap = new();

    public T GetInstance<T>()
    {
        return (T)GetInstance(typeof(T));
    }

    private object GetInstance(Type type)
    {
        if (m_TypeToInstanceMap.TryGetValue(type, out var instance))
            return instance;

        if (m_TypeToFactoryMap.TryGetValue(type, out var factory))
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

    public void Register<T>(Func<object> factory)
    {
        m_TypeToFactoryMap.Add(typeof(T), factory);
    }
    
    public T New<T>()
    {
        return (T)CreateInstance(typeof(T));
    }

    public void Delete<T>(T obj)
    {
        
    }
}