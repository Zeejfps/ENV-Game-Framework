using System.Numerics;

namespace CombatBeesBenchmark;

public sealed class Field
{
    public Vector3 Size { get; } = new Vector3(100f,20f,30f);
    public float Gravity = -20f;
}