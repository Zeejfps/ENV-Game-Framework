using EasyGameFramework.Api.AssetTypes;

namespace EasyGameFramework.Api;

public interface IAssetLoader<T> where T : IAsset
{
    T Load(string assetPath);
}