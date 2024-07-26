namespace OpenGlWrapper.Buffers;

public interface IBufferMemoryRange<T> where T : unmanaged
{
    void Flush();
}