using EasyGameFramework.Api.AssetTypes;

namespace EasyGameFramework.Api.Rendering;

public interface IRenderbufferManager
{
    IGpuFramebufferHandle WindowBufferHandle { get; }
    float Width { get; }
    float Height { get; }

    void Bind(IHandle<IGpuRenderbuffer>? framebuffer);
    void BindToWindow();
    void ClearColorBuffers(float r, float g, float b, float a);
    void SetSize(int width, int height);
    IGpuRenderbufferHandle CreateRenderbuffer(int colorBuffersCount, bool createDepthBuffer, int width, int height);
    void ReleaseRenderbuffer(IGpuRenderbufferHandle tempRenderbufferHandle);
    void Blit(IHandle<IGpuRenderbuffer> src);
    void Blit(IHandle<IGpuRenderbuffer> src, int dstX, int dstY, int dstWidth, int dstHeight);
}