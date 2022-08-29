using EasyGameFramework.API.AssetTypes;

namespace EasyGameFramework.API;

public interface IGpu
{
    bool EnableDepthTest { get; set; }
    bool EnableBackfaceCulling { get; set; }
    bool EnableBlending { get; set; }

    IHandle<IGpuMesh> LoadMesh(string assetPath);
    IHandle<IGpuShader> LoadShader(string assetPath);
    IHandle<IGpuTexture> LoadTexture(string assetPath);
    
    void SaveState();
    void RestoreState();
}