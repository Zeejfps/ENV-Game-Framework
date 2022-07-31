namespace Framework;

public interface IGpuTexture : IAsset
{
    IGpuTextureHandle Use();
}