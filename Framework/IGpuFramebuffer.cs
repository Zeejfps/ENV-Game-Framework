namespace Framework;

public interface IGpuFramebuffer
{
    int Width { get; }
    int Height { get; }

    IGpuFramebufferHandle Use();
}