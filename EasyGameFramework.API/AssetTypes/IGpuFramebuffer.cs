namespace EasyGameFramework.API.AssetTypes;

public interface IGpuFramebuffer : IGpuAsset
{
    int Width { get; }
    int Height { get; }
    
    void Clear(float r, float g, float b, float a);
    void Resize(int width, int height);
}