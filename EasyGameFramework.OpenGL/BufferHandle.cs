using EasyGameFramework.Api;
using EasyGameFramework.Api.Rendering;

namespace EasyGameFramework.OpenGL;

internal sealed class BufferHandle : IHandle<IBuffer>
{
    public Buffer Buffer { get; }
    
    public BufferHandle(Buffer buffer)
    {
        Buffer = buffer;
    }
}