using System.Numerics;
using System.Runtime.InteropServices;

namespace CombatBeesBenchmarkV2.Components;

[StructLayout(LayoutKind.Sequential)]
public struct BeeRenderComponent
{
    public Vector4 Color;
    public Matrix4x4 ModelMatrix;
}