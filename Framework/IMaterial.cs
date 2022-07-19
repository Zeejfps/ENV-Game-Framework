namespace Framework;

public interface IMaterial : IAsset
{
    bool EnableDepthTest { get; set; }
    bool EnableBackfaceCulling { get; set; }
    bool EnableBlending { get; set; }
    
    IMaterialHandle Use();
}