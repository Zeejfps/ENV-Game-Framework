using static GL46;
using static OpenGLSandbox.OpenGlUtils;

namespace OpenGL.NET;

public interface IBuffer<T> where T : unmanaged
{
    uint Target { get; }
}

public enum BufferUsageHint : uint
{
    StreamDraw = GL_STREAM_DRAW,
    StreamRead = GL_STREAM_READ,
    StaticDraw = GL_STATIC_DRAW,
    DynamicDraw = GL_DYNAMIC_DRAW,
    DynamicRead = GL_DYNAMIC_READ,
    DynamicCopy = GL_DYNAMIC_COPY,
}

public readonly struct Buffer<T> : IBuffer<T> where T : unmanaged
{
    public uint Id { get; }
    public uint Target { get; }

    internal Buffer(uint id, uint target)
    {
        Id = id;
        Target = target;
    }
}

public static class GLBuffer
{
    public static Buffer<T> glBindBuffer<T>(uint target, uint bufferId) where T : unmanaged
    {
        var buffer = new Buffer<T>(bufferId, target);
        GL46.glBindBuffer(target, bufferId);
        AssertNoGlError();
        return buffer;
    }

    public static unsafe void glBufferData<T>(
        Buffer<T> buffer,
        ReadOnlySpan<T> data,
        BufferUsageHint usageHint)
        where T : unmanaged
    {
        var target = buffer.Target;
        var sizeOfType = sizeof(T);
        fixed (void* dataPtr = &data[0])
        {
            GL46.glBufferData(target, data.Length * sizeOfType, dataPtr, (uint)usageHint);
        }
    }
}