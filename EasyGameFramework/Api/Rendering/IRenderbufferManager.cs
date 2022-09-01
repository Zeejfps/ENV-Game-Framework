using EasyGameFramework.Api.AssetTypes;

namespace EasyGameFramework.Api.Rendering;

public interface IRenderbufferManager
{
    IGpuFramebufferHandle WindowBufferHandle { get; }
    float Width { get; }
    float Height { get; }

    void Bind(IHandle<IGpuRenderbuffer>? framebuffer);
    void BindWindow();
    void ClearColorBuffers(float r, float g, float b, float a);
    void SetSize(int width, int height);
    IGpuRenderbufferHandle CreateRenderbuffer(int colorBuffersCount, bool createDepthBuffer);
    void ReleaseTempRenderbuffer(IGpuRenderbufferHandle tempRenderbufferHandle);
}