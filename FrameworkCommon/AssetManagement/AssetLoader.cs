namespace Framework;

public abstract class AssetLoader<T> : IAssetLoader<T> where T : IAsset
{
    public T Load(string assetPath)
    {
        if (!File.Exists(assetPath))
            throw new Exception($"File does not exists at path: {assetPath}");

        try
        {
            using var stream = File.Open(assetPath, FileMode.Open);
            return Load(stream);
        }
        catch (Exception e)
        {
            throw new Exception($"Failed to load asset: {assetPath}", e);
        }
    }

    protected abstract T Load(Stream stream);
}