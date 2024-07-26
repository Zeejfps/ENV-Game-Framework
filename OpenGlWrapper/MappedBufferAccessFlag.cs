namespace OpenGlWrapper;

[Flags]
public enum MappedBufferAccessFlag : uint
{
    None = 0,
    Read = GL46.GL_MAP_READ_BIT,
    Write = GL46.GL_MAP_WRITE_BIT,
    ReadWrite = Read | Write,
    PersistentRead = GL46.GL_MAP_PERSISTENT_BIT | Read,
    PersistentWrite = GL46.GL_MAP_PERSISTENT_BIT | Write,
    PersistentReadWrite = GL46.GL_MAP_PERSISTENT_BIT | ReadWrite,
    CoherentPersistentRead = GL46.GL_MAP_COHERENT_BIT | PersistentRead,
    CoherentPersistentWrite = GL46.GL_MAP_COHERENT_BIT | PersistentWrite,
    CoherentPersistentReadWrite = GL46.GL_MAP_COHERENT_BIT | PersistentReadWrite,

    InvalidateRange = GL46.GL_MAP_INVALIDATE_RANGE_BIT,
    InvalidateBuffer = GL46.GL_MAP_INVALIDATE_BUFFER_BIT,
    
    FlushExplicit = GL46.GL_MAP_FLUSH_EXPLICIT_BIT,
    
    Unsynchronized = GL46.GL_MAP_UNSYNCHRONIZED_BIT 
}