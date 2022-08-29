using EasyGameFramework.API.AssetTypes;

namespace EasyGameFramework.API;

public interface IRenderbufferManager
{
    IGpuFramebufferHandle WindowBufferHandle { get; }
    
    void Use(IHandle<IGpuRenderbuffer>? framebuffer);
    void UseWindow();
    void ClearColor(float r, float g, float b, float a);
    void SetSize(int width, int height);
}