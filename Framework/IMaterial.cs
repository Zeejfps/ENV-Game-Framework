namespace Framework;

public interface IMaterial : IAsset
{
    bool UseDepthTest { get; set; }
    bool UseBackfaceCulling { get; set; }

    IMaterialApi Use();
}