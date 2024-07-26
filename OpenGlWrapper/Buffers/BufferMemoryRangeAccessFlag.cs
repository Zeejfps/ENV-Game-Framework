using static GL46;

namespace OpenGlWrapper.Buffers;

[Flags]
public enum BufferMemoryRangeAccessFlag : uint
{
    None = 0,
    
    Persistent = GL_MAP_PERSISTENT_BIT,
    Coherent = GL_MAP_COHERENT_BIT | Persistent,

    InvalidateRange = GL_MAP_INVALIDATE_RANGE_BIT,
    InvalidateBuffer = GL_MAP_INVALIDATE_BUFFER_BIT,
    
    FlushExplicit = GL_MAP_FLUSH_EXPLICIT_BIT,
    
    Unsynchronized = GL_MAP_UNSYNCHRONIZED_BIT 
}