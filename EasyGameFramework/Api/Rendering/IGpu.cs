namespace EasyGameFramework.Api.Rendering;

public interface IGpu
{
    bool EnableDepthTest { get; set; }
    bool EnableBackfaceCulling { get; set; }
    bool EnableBlending { get; set; }

    IMeshController MeshController { get; }
    IShaderController ShaderController { get; }
    ITextureController TextureController { get; }
    IRenderbufferManager FramebufferController { get; }
    IBufferController BufferController { get; }
    IPipelineController PipelineController { get; }

    IGpuRenderbufferHandle CreateRenderbuffer(
        int colorBuffersCount, 
        bool createDepthBuffer,
        int width, int height,
        TextureFilterKind filterMode = TextureFilterKind.Linear);
    
    void ReleaseRenderbuffer(IGpuRenderbufferHandle tempRenderbufferHandle);
    
    void SaveState();
    void RestoreState();
    
    IHandle<IPipeline> CreatePipeline();
}