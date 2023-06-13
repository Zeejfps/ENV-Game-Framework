using EasyGameFramework.Api;
using EasyGameFramework.Api.Rendering;

namespace EasyGameFramework.OpenGL;

internal sealed class BufferHandle : IHandle<IBuffer>
{
    public BufferKind Kind { get; }
    public uint Id { get; }
    public int BufferSizeInBytes { get; }

    public BufferHandle(BufferKind kind, uint id, int bufferSizeInBytes)
    {
        Kind = kind;
        Id = id;
        BufferSizeInBytes = bufferSizeInBytes;
    }
}