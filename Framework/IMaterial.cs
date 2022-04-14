namespace Framework;

public interface IMaterial : IAsset
{
    bool EnableDepthTest { get; set; }
    bool EnableBackfaceCulling { get; set; }
    bool EnableBlending { get; set; }
    
    IMaterialApi Use();
}