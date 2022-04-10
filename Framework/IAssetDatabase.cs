namespace Framework;

public interface IAssetDatabase
{
    T LoadAsset<T>(string assetPath) where T : IAsset;
}