namespace OpenGlWrapper.Buffers;

public interface IWriteOnlyBufferMemory<T> : IBufferMemory<T> where T : unmanaged
{
    void Write(int index, T data);
}