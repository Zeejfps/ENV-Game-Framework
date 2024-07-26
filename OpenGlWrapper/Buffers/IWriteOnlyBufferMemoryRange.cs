namespace OpenGlWrapper.Buffers;

public interface IWriteOnlyBufferMemoryRange<T> : IWriteOnlyBufferMemory<T>, IBufferMemoryRange<T> where T : unmanaged { }