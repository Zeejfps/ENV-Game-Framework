using System.Numerics;
using System.Runtime.InteropServices;
using OpenGL.NET;

namespace SoftwareRendererModule;

[StructLayout(LayoutKind.Sequential)]
public readonly struct MyVertex
{
    [VertexAttrib(3, typeof(float))]
    public readonly Vector3 Position;

    [VertexAttrib(2, typeof(float))]
    public readonly Vector2 TextureCoords;
}