using EasyGameFramework.Api;

namespace EasyGameFramework.Builder;

public sealed class DiContainer : IContainer
{
    private readonly Dictionary<Type, Func<object>> m_TypeToFactoryMap = new();
    private readonly Dictionary<Type, object> m_TypeToInstanceMap = new();

    public T New<T>()
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
            return instance;
        }

        if (!type.IsAbstract)
        {
            instance = CreateInstance(type);
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

    public void BindSingleton<TInterface, TConcrete>() where TConcrete : TInterface
    {
        m_TypeToFactoryMap.Add(typeof(TInterface), () =>
        {
            var interfaceType = typeof(TInterface);
            var concreteType = typeof(TConcrete);
            if (!m_TypeToInstanceMap.TryGetValue(concreteType, out var instance))
            {
                instance = CreateInstance(concreteType);
                m_TypeToInstanceMap[concreteType] = instance;
            }
            m_TypeToInstanceMap[interfaceType] = instance;
            return instance;
        });
    }

    public void BindFactory<T>(Func<object> factory)
    {
        m_TypeToFactoryMap.Add(typeof(T), factory);
    }

    public void BindSingleton<T>(T instance)
    {
        var type = typeof(T);
        m_TypeToInstanceMap[type] = instance;
    }
}