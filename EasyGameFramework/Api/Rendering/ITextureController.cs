namespace EasyGameFramework.Api.Rendering;

public interface ITextureController
{
    IGpuTextureHandle Load(string assetPath, TextureFilterKind filter = TextureFilterKind.Linear);
    void Bind(IGpuTextureHandle handle);
    void Upload(ReadOnlySpan<byte> pixels);
    void SaveState();
    void RestoreState();
}