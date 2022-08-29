using EasyGameFramework.API.AssetTypes;

namespace EasyGameFramework.API;

public interface IMaterial
{
    void Apply(IGpuShader shader);
}