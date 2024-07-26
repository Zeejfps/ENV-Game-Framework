namespace OpenGLSandbox;

using static GL46;
using static OpenGlUtils;

[Flags]
public enum AccessFlag : uint
{
    None = 0,
    Dynamic = GL_DYNAMIC_STORAGE_BIT,
    Read = GL_MAP_READ_BIT,
    Write = GL_MAP_WRITE_BIT,
    PersistentRead = GL_MAP_PERSISTENT_BIT | Read,
    PersistentWrite = GL_MAP_PERSISTENT_BIT | Write,
    PersistentReadWrite = GL_MAP_PERSISTENT_BIT | Read | Write,
    CoherentPersistentRead = GL_MAP_COHERENT_BIT | PersistentRead,
    CoherentPersistentWrite = GL_MAP_COHERENT_BIT | PersistentWrite,
    CoherentPersistentReadWrite = GL_MAP_COHERENT_BIT | PersistentReadWrite,
    //Client = GL_CLIENT_STORAGE_BIT,
}

public interface IImmutableBuffer<T> : IBuffer
{
    int Size { get; set; }
    bool IsAllocated { get; set; }
}

public static class ImmutableBufferMethods
{
    public static void Alloc<T>(this IImmutableBuffer<T> buffer, ReadOnlySpan<T> data, AccessFlag accessFlags = AccessFlag.None) where T : unmanaged
    {
        unsafe
        {
            fixed (void* ptr = &data[0])
            {
                buffer.AllocUnsafe(data.Length, ptr, accessFlags);
            }
        }
    }
    
    private static unsafe void AllocUnsafe<T>(this IImmutableBuffer<T> buffer, int size, void* data, AccessFlag flags) where T : unmanaged
    {
        var sizePtr = SizeOf<T>(size);
        buffer.Id = glGenBuffer();
        
        glBindBuffer(buffer.BindTarget, buffer.Id);
        AssertNoGlError();
        glBufferStorage(buffer.BindTarget, sizePtr, data, (uint)flags);
        AssertNoGlError();
        
        buffer.Size = size;
        buffer.IsAllocated = true;
    }
}