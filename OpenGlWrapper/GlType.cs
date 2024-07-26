using static GL46;

namespace OpenGlWrapper;

public enum GlType : uint
{
    Byte = GL_BYTE,
    UByte = GL_UNSIGNED_BYTE,
    Short = GL_SHORT,
    UShort = GL_UNSIGNED_SHORT,
    Int = GL_INT,
    UInt = GL_UNSIGNED_INT,
    Float = GL_FLOAT,
    HalfFloat = GL_HALF_FLOAT,
    Double = GL_DOUBLE,
}