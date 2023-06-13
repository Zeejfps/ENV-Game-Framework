using EasyGameFramework.Api.Rendering;
using OpenGL;

namespace EasyGameFramework.OpenGL;

internal static class BufferUsageExtensions
{
    public static int ToOpenGl(this BufferUsage usage)
    {
        switch (usage)
        {
            case BufferUsage.StaticDraw:
                return Gl.GL_STATIC_DRAW;
            case BufferUsage.DynamicDraw:
                return Gl.GL_DYNAMIC_DRAW;
            default:
                throw new ArgumentOutOfRangeException(nameof(usage), usage, null);
        }
    }
}