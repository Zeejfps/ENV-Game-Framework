using EasyGameFramework.Api;
using EasyGameFramework.Api.Rendering;
using static OpenGL.Gl;

namespace EasyGameFramework.OpenGL;

internal sealed class BufferController : IBufferController
{
    private BufferHandle Buffer { get; set; }
    
    public void Bind(IHandle<IBuffer> bufferHandle)
    {
        var buffer = (BufferHandle)bufferHandle;
        Buffer = buffer;
        glBindBuffer(buffer.Kind.ToOpenGl(), buffer.Id);
    }

    public void Upload<T>(ReadOnlySpan<T> data) where T : unmanaged
    {
        unsafe
        {
            fixed (void* p = &data[0])
            {
                glBufferSubData(Buffer.Kind.ToOpenGl(), 0, sizeof(T) * data.Length, p);
            }
        }
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