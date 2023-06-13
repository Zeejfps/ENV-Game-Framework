using EasyGameFramework.Api.Rendering;
using static OpenGL.Gl;

namespace EasyGameFramework.OpenGL;

internal static class BufferKindExtensions
{
    public static int ToOpenGl(this BufferKind kind)
    {
        switch (kind)
        {
            case BufferKind.ArrayBuffer:
                return GL_ARRAY_BUFFER;
            case BufferKind.UniformBuffer:
                return GL_UNIFORM_BUFFER;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
        }
    }
}