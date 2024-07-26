using static GL46;

namespace OpenGlWrapper;

[Flags]
public enum FixedSizedBufferAccessFlag : uint
{
    None = 0,
    Dynamic = GL_DYNAMIC_STORAGE_BIT,
    Read = GL_MAP_READ_BIT,
    Write = GL_MAP_WRITE_BIT,
    ReadWrite = Read | Write,
    PersistentRead = GL_MAP_PERSISTENT_BIT | Read,
    PersistentWrite = GL_MAP_PERSISTENT_BIT | Write,
    PersistentReadWrite = GL_MAP_PERSISTENT_BIT | ReadWrite,
    CoherentPersistentRead = GL_MAP_COHERENT_BIT | PersistentRead,
    CoherentPersistentWrite = GL_MAP_COHERENT_BIT | PersistentWrite,
    CoherentPersistentReadWrite = GL_MAP_COHERENT_BIT | PersistentReadWrite,
    //Client = GL_CLIENT_STORAGE_BIT,
}