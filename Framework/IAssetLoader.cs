namespace Framework;

public interface IAssetLoader<T> where T : IAsset
{
    T Load(string assetPath);
}