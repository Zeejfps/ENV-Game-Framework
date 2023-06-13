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
    void Blit(IHandle<IGpuRenderbuffer> src, TextureFilterKind filter = TextureFilterKind.Linear);
    void Blit(IHandle<IGpuRenderbuffer> src, int left, int bottom, int right, int top, TextureFilterKind filter = TextureFilterKind.Linear);
    void Blit(IHandle<IGpuRenderbuffer> fb, IViewport viewport, TextureFilterKind filter = TextureFilterKind.Linear);
}