using EasyGameFramework.API.AssetTypes;

namespace EasyGameFramework.API;

public interface IGpu
{
    IHandle<IGpuShader> LoadShader(string shaderPath);
}