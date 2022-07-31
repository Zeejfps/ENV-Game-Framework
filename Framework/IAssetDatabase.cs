namespace Framework;

public interface IAssetDatabase
{
    T Load<T>(string assetPath) where T : IAsset;
}