namespace OpenGlWrapper.Buffers;

public interface IBufferMemory<T> : IDisposable where T : unmanaged
{
    int Count { get; }
}