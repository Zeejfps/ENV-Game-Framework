using EasyGameFramework.API;

namespace EasyGameFramework.AssetManagement;

public class Locator : ILocator
{
    private readonly Dictionary<Type, object> m_TypeToObjectMap = new();

    public T? Locate<T>()
    {
        var type = typeof(T);
        if (!m_TypeToObjectMap.TryGetValue(type, out var singleton))
            return default;
        return (T)singleton;
    }

    public T LocateOrThrow<T>()
    {
        var type = typeof(T);
        if (!m_TypeToObjectMap.TryGetValue(type, out var singleton))
            throw new Exception($"Could not locate object for type: {type}");
        return (T)singleton;
    }

    public void RegisterSingleton<T>(T singleton)
    {
        if (singleton == null)
            throw new ArgumentNullException(nameof(singleton));
        
        var type = typeof(T);
        if (m_TypeToObjectMap.ContainsKey(type))
            throw new Exception($"An object is already registered for type: {type}");
        
        m_TypeToObjectMap[type] = singleton;
    }
}