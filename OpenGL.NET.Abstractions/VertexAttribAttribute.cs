namespace OpenGL.NET;

using static GL46;

// public enum VertexAttribType : uint
// {
//     Byte = GL_BYTE,
//     UnsignedByte = GL_UNSIGNED_BYTE,
//     Short = GL_SHORT,
//     UnsignedShort = GL_UNSIGNED_SHORT,
//     Int = GL_INT,
//     UnsignedInt = GL_UNSIGNED_INT,
//     
//     HalfFloat = GL_HALF_FLOAT,
//     Float = GL_FLOAT,
//     Double = GL_DOUBLE,
//     Fixed = GL_FIXED,
// }

[AttributeUsage(AttributeTargets.Field)]
public sealed class VertexAttribAttribute : Attribute
{
    public VertexAttribAttribute(int count, Type type)
    {
        Count = count;
        Type = type;
    }

    public int Count { get; }
    public Type Type { get; }

}