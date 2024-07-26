namespace OpenGlWrapper.Buffers;

public interface IReadWriteBufferMemoryRange<T> : IReadWriteBufferMemory<T>, IBufferMemoryRange<T> where T : unmanaged { }