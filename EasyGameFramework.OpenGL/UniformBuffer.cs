using EasyGameFramework.Api.Rendering;
using static OpenGL.Gl;

namespace EasyGameFramework.OpenGL;

internal sealed class UniformBuffer : IBuffer
{
    public uint Id { get; }
    public int SizeInBytes { get; }

    private UniformBuffer(uint id, int sizeInBytes)
    {
        Id = id;
        SizeInBytes = sizeInBytes;
    }

    public void Put<T>(T data) where T : unmanaged
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

    public void Dispose()
    {
    }

    public static UniformBuffer Create(BufferUsage usage, int sizeInBytes)
    {
        var bufferId = glGenBuffer();

        glBindBuffer(GL_UNIFORM_BUFFER, bufferId);
        glAssertNoError();

        glBufferData(GL_UNIFORM_BUFFER, sizeInBytes, IntPtr.Zero, usage.ToOpenGl());
        glAssertNoError();

        return new UniformBuffer(bufferId, sizeInBytes);
    }
}