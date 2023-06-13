using EasyGameFramework.Api;
using EasyGameFramework.Api.Rendering;

namespace EasyGameFramework.OpenGL;

internal sealed class BufferHandle : IHandle<IBuffer>
{
    public BufferKind Kind { get; }
    public uint BufferId { get; }
    public int BufferSizeInBytes { get; }

    public BufferHandle(BufferKind kind, uint bufferId, int bufferSizeInBytes)
    {
        Kind = kind;
        BufferId = bufferId;
        BufferSizeInBytes = bufferSizeInBytes;
    }
}