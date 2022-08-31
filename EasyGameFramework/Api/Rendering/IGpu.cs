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

    void SaveState();
    void RestoreState();
}