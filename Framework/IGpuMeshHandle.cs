namespace Framework;

public interface IGpuMeshHandle : IDisposable
{
    void Render();
    void RenderInstanced(int instanceCount);
}