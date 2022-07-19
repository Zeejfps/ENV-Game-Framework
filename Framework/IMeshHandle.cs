namespace Framework;

public interface IMeshHandle : IDisposable
{
    void Render();
    void RenderInstanced(int instanceCount);
}