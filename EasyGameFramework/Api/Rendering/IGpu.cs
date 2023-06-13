namespace EasyGameFramework.Api.Rendering;

public interface IGpu
{
    bool EnableDepthTest { get; set; }
    bool EnableBackfaceCulling { get; set; }
    bool EnableBlending { get; set; }

    IMeshManager Mesh { get; }
    IShaderManager Shader { get; }
    ITextureManager Texture { get; }
    IRenderbufferManager Renderbuffer { get; }
    IBufferController BufferController { get; }
    IPipelineController PipelineController { get; }

    IGpuRenderbufferHandle CreateRenderbuffer(int colorBuffersCount, bool createDepthBuffer, int width, int height);
    void ReleaseRenderbuffer(IGpuRenderbufferHandle tempRenderbufferHandle);
    
    void SaveState();
    void RestoreState();
    
    IHandle<IPipeline> CreatePipeline();
}