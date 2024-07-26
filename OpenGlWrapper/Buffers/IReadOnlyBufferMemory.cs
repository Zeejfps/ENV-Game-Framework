namespace OpenGlWrapper.Buffers;

public interface IReadOnlyBufferMemory<T> : IBufferMemory<T> where T : unmanaged
{
    T Read(int index);
}