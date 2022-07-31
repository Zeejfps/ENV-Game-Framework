namespace Framework;

public interface IGpuFramebuffer : IGpuAsset
{
    int Width { get; }
    int Height { get; }

    IGpuFramebufferHandle Use();
}