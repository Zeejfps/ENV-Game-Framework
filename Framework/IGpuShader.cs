namespace Framework;

public interface IGpuShader : IAsset
{
    bool EnableDepthTest { get; set; }
    bool EnableBackfaceCulling { get; set; }
    bool EnableBlending { get; set; }
    
    IGpuShaderHandle Use();
}