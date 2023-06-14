using EasyGameFramework.Api.Rendering;
using OpenGL;

namespace EasyGameFramework.OpenGL;

public static class TextureFilterKindExtensions {

    public static int ToOpenGl(this TextureFilterKind mode)
    {
        return mode switch
        {
            TextureFilterKind.Nearest => Gl.GL_NEAREST,
            TextureFilterKind.Linear => Gl.GL_LINEAR,
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };
    }
    
}