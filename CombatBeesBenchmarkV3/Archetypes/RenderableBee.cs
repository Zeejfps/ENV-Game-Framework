using System.Numerics;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
public struct RenderableBee
{
    public Vector4 Color;
    public Matrix4x4 ModelMatrix;
}