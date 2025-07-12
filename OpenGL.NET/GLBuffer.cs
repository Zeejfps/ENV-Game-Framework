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
    public uint Type { get; }

    internal Buffer(uint id, uint target, uint type)
    {
        Id = id;
        Target = target;
        Type = type;
    }
}

public static class GLBuffer
{
    public static Buffer<T> glBindBuffer<T>(uint target, uint bufferId) where T : unmanaged
    {
        var glType = GetGlType(typeof(T), out var sizeOfT);
        var buffer = new Buffer<T>(bufferId, target, glType);
        GL46.glBindBuffer(target, bufferId);
        AssertNoGlError();
        return buffer;
    }

    public static unsafe void glBufferData<TData>(
        Buffer<TData> buffer,
        ReadOnlySpan<TData> data,
        BufferUsageHint usageHint)
        where TData : unmanaged
    {
        var target = buffer.Target;
        var sizeOfType = sizeof(TData);
        fixed (void* dataPtr = &data[0])
        {
            GL46.glBufferData(target, data.Length * sizeOfType, dataPtr, (uint)usageHint);
            AssertNoGlError();
        }
    }
}