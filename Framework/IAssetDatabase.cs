namespace ENV.Engine;

public interface IAssetDatabase
{
    T LoadAsset<T>(string assetPath) where T : IAsset;
}