using EasyGameFramework.API.AssetTypes;

namespace EasyGameFramework.API;

public interface IGpu
{
    IHandle<IGpuTexture> LoadTexture(string assetPath);

    IHandle<IGpuShader> LoadShader(string shaderPath);
}