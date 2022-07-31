namespace Framework;

public interface IGpuFramebuffer : IAsset
{
    int Width { get; }
    int Height { get; }

    IGpuFramebufferHandle Use();
}