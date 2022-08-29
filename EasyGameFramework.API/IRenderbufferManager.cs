using EasyGameFramework.API.AssetTypes;

namespace EasyGameFramework.API;

public interface IRenderbufferManager
{
    int FramebufferWidth { get; }
    int FramebufferHeight { get; }
    IHandle<IGpuTexture>[] ColorBuffers { get; }
    IHandle<IGpuTexture>? DepthBuffer { get; }
    
    void Use(IHandle<IGpuRenderbuffer>? framebuffer);
    void ClearColor(float r, float g, float b, float a);
    void SetSize(int width, int height);
}