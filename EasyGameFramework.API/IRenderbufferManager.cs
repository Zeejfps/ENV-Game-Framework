using EasyGameFramework.API.AssetTypes;

namespace EasyGameFramework.API;

public interface IRenderbufferManager
{
    IGpuFramebufferHandle WindowBufferHandle { get; }
    
    void Bind(IHandle<IGpuRenderbuffer>? framebuffer);
    void BindWindow();
    void ClearColorBuffer(float r, float g, float b, float a);
    void SetSize(int width, int height);
}