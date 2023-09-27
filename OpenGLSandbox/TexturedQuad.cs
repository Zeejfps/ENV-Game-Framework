using System.Numerics;
using System.Runtime.InteropServices;

namespace OpenGLSandbox;

[StructLayout(LayoutKind.Sequential)]
public readonly struct TexturedQuad
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Vertex
    {
        public Vector2 Position;
        public Vector2 TexCoords;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    public struct Triangle
    {
        public Vertex V1;
        public Vertex V2;
        public Vertex V3;
    }
    
    public readonly Triangle T1 = new()
    {
        V1 =
        {
            Position = new Vector2(-1f, -1f),
            TexCoords = new Vector2(0f, 0f)
        },
        V2 =
        {
            Position = new Vector2(1f, -1f),
            TexCoords = new Vector2(1f, 0f)
        },
        V3 =
        {
            Position = new Vector2(-1f, 1f),
            TexCoords = new Vector2(0f, 1f)
        }
    };
    
    public readonly Triangle T2 = new()
    {
        V1 =
        {
            Position = new Vector2(1f, -1f),
            TexCoords = new Vector2(1f, 0f)
        },
        V2 =
        {
            Position = new Vector2(1f, 1f),
            TexCoords = new Vector2(1f, 1f)
        },
        V3 =
        {
            Position = new Vector2(-1f, 1f),
            TexCoords = new Vector2(0f, 1f)
        }
    };

    public TexturedQuad(){}
}