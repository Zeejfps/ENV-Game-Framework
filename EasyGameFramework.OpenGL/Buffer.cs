using EasyGameFramework.Api.Rendering;
using static OpenGL.Gl;

namespace EasyGameFramework.OpenGL;

internal sealed class Buffer : IBuffer
{
    public uint Id { get; }
    public int SizeInBytes { get; }

    public Buffer(uint id, int sizeInBytes)
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
}