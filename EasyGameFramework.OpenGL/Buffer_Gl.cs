using EasyGameFramework.Api;
using EasyGameFramework.Api.Rendering;
using static OpenGL.Gl;

namespace EasyGameFramework.OpenGL;

internal sealed class Buffer_Gl : IHandle<IBuffer>, IShaderStorageBufferHandle
{
    public int Target { get; }
    public uint Id { get; }
    public int BufferSizeInBytes { get; }

    public Buffer_Gl(uint id, int target, int bufferSizeInBytes)
    {
        Id = id;
        Target = target;
        BufferSizeInBytes = bufferSizeInBytes;
    }

    public void Bind()
    {
        glBindBuffer(Target, Id);
    }

    public void Upload<T>(ReadOnlySpan<T> data) where T : unmanaged
    {
        unsafe
        {
            fixed (void* p = &data[0])
            {
                glBufferSubData(Target, 0, sizeof(T) * data.Length, p);
                glAssertNoError();
            }
        }
    }
}