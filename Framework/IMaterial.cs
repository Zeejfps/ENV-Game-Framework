namespace Framework;

public interface IMaterial : IAsset
{
    bool IsDepthTestEnabled { get; set; }
    bool IsBackfaceCullingEnabled { get; set; }

    IMaterialApi Use();
}