using System.Diagnostics;
using EasyGameFramework.Api;
using EasyGameFramework.Api.Rendering;
using static OpenGL.Gl;

namespace EasyGameFramework.OpenGL;

internal sealed class BufferController : IBufferController
{
    private Buffer_Gl Buffer { get; set; }
    
    public void Bind(IHandle<IBuffer> bufferHandle)
    {
        Buffer = (Buffer_Gl)bufferHandle;
        Buffer.Bind();
    }

    public void Upload<T>(ReadOnlySpan<T> data) where T : unmanaged
    {
        Debug.Assert(Buffer != null);
        Buffer.Upload(data);
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

    public IShaderStorageBufferHandle CreateAndBindShaderStorageBuffer(BufferUsage usage, int sizeInBytes)
    {
        var bufferId = glGenBuffer();
        glAssertNoError();
        
        glBindBuffer(GL_SHADER_STORAGE_BUFFER, bufferId);
        glAssertNoError();
        
        glBufferData(GL_SHADER_STORAGE_BUFFER, sizeInBytes, IntPtr.Zero, usage.ToOpenGl());
        glAssertNoError();
        
        return new Buffer_Gl(bufferId, GL_SHADER_STORAGE_BUFFER, sizeInBytes);
    }

    private IHandle<IBuffer> CreateUniformBuffer(BufferUsage usage, int sizeInBytes)
    {
        var bufferId = glGenBuffer();

        glBindBuffer(GL_UNIFORM_BUFFER, bufferId);
        glAssertNoError();

        glBufferData(GL_UNIFORM_BUFFER, sizeInBytes, IntPtr.Zero, usage.ToOpenGl());
        glAssertNoError();

        return new Buffer_Gl(bufferId, GL_UNIFORM_BUFFER, sizeInBytes);
    }
}