namespace OpenGlWrapper;

[Flags]
public enum ImmutableBufferAccessFlag : uint
{
    None = 0,
    Dynamic = GL46.GL_DYNAMIC_STORAGE_BIT,
    Read = GL46.GL_MAP_READ_BIT,
    Write = GL46.GL_MAP_WRITE_BIT,
    PersistentRead = GL46.GL_MAP_PERSISTENT_BIT | Read,
    PersistentWrite = GL46.GL_MAP_PERSISTENT_BIT | Write,
    PersistentReadWrite = GL46.GL_MAP_PERSISTENT_BIT | Read | Write,
    CoherentPersistentRead = GL46.GL_MAP_COHERENT_BIT | PersistentRead,
    CoherentPersistentWrite = GL46.GL_MAP_COHERENT_BIT | PersistentWrite,
    CoherentPersistentReadWrite = GL46.GL_MAP_COHERENT_BIT | PersistentReadWrite,
    //Client = GL_CLIENT_STORAGE_BIT,
}