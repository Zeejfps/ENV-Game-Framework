using EasyGameFramework.API.AssetTypes;

namespace EasyGameFramework.API;

public interface IAssetLoader<T> where T : IAsset
{
    T Load(string assetPath);
}