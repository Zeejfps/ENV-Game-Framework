namespace OpenGlWrapper.Buffers;

public interface IBufferMemory<T> : IDisposable where T : unmanaged
{
    int Length { get; }
}