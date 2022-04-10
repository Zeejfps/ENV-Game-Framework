namespace ENV.Engine;

public interface IAssetLoader
{
    T LoadAsset<T>(string assetPath) where T : IAsset;
}