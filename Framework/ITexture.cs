namespace Framework;

public interface ITexture : IAsset
{
    ITextureHandle Use();
}