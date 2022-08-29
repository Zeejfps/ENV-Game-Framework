namespace EasyGameFramework.API.AssetTypes;

public interface IGpuFramebuffer : IAsset
{
    int Width { get; }
    int Height { get; }

    IGpuFramebufferHandle Use();
}