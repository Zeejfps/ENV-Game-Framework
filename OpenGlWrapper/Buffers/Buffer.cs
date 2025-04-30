using static GL46;
using static OpenGlWrapper.OpenGlUtilsTwo;

namespace OpenGlWrapper.Buffers;

internal abstract class Buffer
{
    public FixedSizedBufferAccessFlag AccessFlags { get; protected set; } = FixedSizedBufferAccessFlag.None;
    public abstract uint Kind { get; }
    public bool IsAllocated { get; set; }
    public bool IsFixedSize { get; set; }
    public int SizeInBytes { get; set; }
    
    protected abstract uint Id { get; }
    
    public void Bind()
    {
        glBindBuffer(Kind, Id);
        AssertNoGlError();
    }
    
    public IWriteOnlyBufferMemory<T> MapWrite<T>() where T : unmanaged
    {
        if ((AccessFlags & FixedSizedBufferAccessFlag.Write) == 0)
            throw new InvalidOperationException($"Can't map buffer for write access, missing access flags. Required Write flag, found: {AccessFlags}");
        return MapInternal<T>(SizeInBytes, GL_WRITE_ONLY);
    }
    
    public IReadOnlyBufferMemory<T> MapRead<T>() where T : unmanaged
    {
        if ((AccessFlags & FixedSizedBufferAccessFlag.Read) == 0)
            throw new InvalidOperationException($"Can't map buffer for read access, missing access flags. Required Read flag, found: {AccessFlags}");
        return MapInternal<T>(SizeInBytes, GL_READ_ONLY);
    }
    
    public IReadWriteBufferMemory<T> MapReadWrite<T>() where T : unmanaged
    {
        if ((AccessFlags & FixedSizedBufferAccessFlag.Read) == 0 ||
            (AccessFlags & FixedSizedBufferAccessFlag.Write) == 0)
            throw new InvalidOperationException($"Can't map buffer for read-write access, missing access flags. Required Read and Write flag, found: {AccessFlags}");

        return MapInternal<T>(SizeInBytes, GL_READ_WRITE);
    }
    
    private unsafe BufferMemoryRange<T> MapInternal<T>(int sizeInBytes, uint access) where T : unmanaged
    {
        var bufferKind = Kind;
        var ptr = glMapBuffer(bufferKind, access);
        AssertNoGlError();
        if (ptr == null)
            throw new Exception("Failed to map buffer, Unknown error");
            
        var sizeOfT = SizeOf<T>();
        var count = sizeInBytes / sizeOfT.ToInt32();
        return new BufferMemoryRange<T>(bufferKind, 0, ptr, count, (uint)BufferMemoryRangeAccessFlag.None);
    }
}