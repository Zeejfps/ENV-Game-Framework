using EasyGameFramework.Api;
using EasyGameFramework.Api.Rendering;
using static OpenGL.Gl;

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

    public void Upload()
    {
        throw new NotImplementedException();
    }

    public IHandle<IBuffer> CreateAndBind(BufferKind kind, BufferUsage usage, int sizeInBytes)
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

    private IHandle<IBuffer> CreateUniformBuffer(BufferUsage usage, int sizeInBytes)
    {
        var bufferId = glGenBuffer();

        glBindBuffer(GL_UNIFORM_BUFFER, bufferId);
        glAssertNoError();

        glBufferData(GL_UNIFORM_BUFFER, sizeInBytes, IntPtr.Zero, usage.ToOpenGl());
        glAssertNoError();

        return new BufferHandle(BufferKind.UniformBuffer, bufferId, sizeInBytes);
    }
}