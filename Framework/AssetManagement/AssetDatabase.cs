namespace Framework;

public class AssetDatabase : IAssetDatabase
{
    private readonly Dictionary<Type, IAssetLoader> m_TypeToLoaderMap = new();
    private readonly Dictionary<string, IAsset> m_PathToAssetMap = new();

    public T Load<T>(string assetPath) where T : IAsset
    {
        if (m_PathToAssetMap.TryGetValue(assetPath, out var asset))
            return (T)asset;
        
        var assetType = typeof(T);
        if (m_TypeToLoaderMap.TryGetValue(assetType, out var module))
        {
            asset = module.LoadAsset(assetPath);
            m_PathToAssetMap[assetPath] = asset;
            return (T)asset;
        }

        throw new Exception($"Could not find Loader for asset type: {assetType}");
    }

    public void AddLoader<T>(IAssetLoader<T> assetLoader) where T : IAsset
    {
        var assetType = typeof(T);
        m_TypeToLoaderMap[assetType] = assetLoader;
    }
}

public interface IAssetLoader
{
    IAsset LoadAsset(string assetPath);
}

public interface IAssetLoader<T> : IAssetLoader where T : IAsset
{
    
}