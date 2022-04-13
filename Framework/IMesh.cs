using System.Numerics;

namespace Framework;

public interface IMesh : IAsset
{
    IMeshApi Use();
}

public interface IMeshApi : IDisposable
{
    void Render();
    void RenderInstanced(Matrix4x4[] transforms);
}