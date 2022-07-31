namespace Framework;

public interface IAssetService
{
    T Load<T>(string assetPath) where T : IAsset;
    T Convert<T>(IAsset asset);
}