namespace OpenGlWrapper.Buffers;

public interface IReadWriteBufferMemory<T> : IReadOnlyBufferMemory<T>, IWriteOnlyBufferMemory<T> where T : unmanaged
{
    Span<T> Span { get; }
}