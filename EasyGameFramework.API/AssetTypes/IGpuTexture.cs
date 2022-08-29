namespace EasyGameFramework.API.AssetTypes;

public interface IGpuTexture : IAsset
{
    IGpuTextureHandle Use();
}