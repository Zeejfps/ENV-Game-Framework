namespace EasyGameFramework.API.AssetTypes;

public interface IGpuTexture : IGpuAsset
{
    IGpuTextureHandle Use();
}