namespace OpenGlWrapper.Buffers;

public interface IReadOnlyBufferMemoryRange<T> : IReadOnlyBufferMemory<T>, IBufferMemoryRange<T> where T : unmanaged { }