using EasyGameFramework.API.AssetTypes;

namespace EasyGameFramework.API;

public interface IGpu
{
    bool EnableDepthTest { get; set; }
    bool EnableBackfaceCulling { get; set; }
    bool EnableBlending { get; set; }
    
    IHandle<IGpuShader> LoadShader(string shaderPath);
    
    void SaveState();
    void RestoreState();
}