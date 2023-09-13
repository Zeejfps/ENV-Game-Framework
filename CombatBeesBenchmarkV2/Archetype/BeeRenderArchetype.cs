using System.Numerics;
using System.Runtime.InteropServices;

namespace CombatBeesBenchmarkV2.Archetype;

[StructLayout(LayoutKind.Sequential)]
public struct BeeRenderArchetype
{
    public Vector4 Color;
    public Matrix4x4 ModelMatrix;
}