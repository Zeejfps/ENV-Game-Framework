using EasyGameFramework.Api;
using EasyGameFramework.Api.Rendering;

namespace EasyGameFramework.OpenGL;

internal sealed class BufferController : IBufferController
{
    public void Bind(IHandle<IBuffer> bufferHandle)
    {
        throw new NotImplementedException();
    }

    public void Put<T>(ReadOnlySpan<T> data) where T : unmanaged
    {
        throw new NotImplementedException();
    }

    public void Write()
    {
        throw new NotImplementedException();
    }

    public IHandle<IBuffer> CreateAndBind(BufferKind kind, BufferUsage usage, int sizeInBytes)
    {
        throw new NotImplementedException();
    }

    public IHandle<IBuffer> CreateBuffer(BufferKind kind, BufferUsage usage, int sizeInBytes)
    {
        switch (kind)
        {
            case BufferKind.ArrayBuffer:
                break;
            case BufferKind.UniformBuffer:
                return CreateUniformBuffer(usage, sizeInBytes);
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
        }
        return null;
    }
    
    private IBufferHandle CreateUniformBuffer(BufferUsage usage, int sizeInBytes)
    {
        var buffer = UniformBuffer.Create(usage, sizeInBytes);
        return null;
    }
}