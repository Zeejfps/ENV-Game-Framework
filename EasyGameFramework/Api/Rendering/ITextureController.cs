namespace EasyGameFramework.Api.Rendering;

public interface ITextureController
{
    IGpuTextureHandle Load(string assetPath, TextureFilterKind filter = TextureFilterKind.Linear, TextureKind kind = TextureKind.Texture2D);
    void Bind(IGpuTextureHandle handle);
    void Upload(ReadOnlySpan<byte> pixels);
    void SaveState();
    void RestoreState();
}