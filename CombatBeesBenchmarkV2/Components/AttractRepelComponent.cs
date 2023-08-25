using System.Numerics;

namespace CombatBeesBenchmarkV2.Components;

public struct AttractRepelComponent
{
    public int TeamIndex;
    public Vector3 AttractionPoint;
    public Vector3 RepellentPoint;
    public Vector3 MoveDirection;
}