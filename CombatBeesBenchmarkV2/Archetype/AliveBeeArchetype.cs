using System.Numerics;

namespace CombatBeesBenchmarkV2.Archetype;

public struct AliveBeeArchetype
{
    public MovementArchetype Movement;
    public Vector3 MoveDirection;
    public Vector3 AttractionPoint;
    public Vector3 RepellentPoint;
    public Vector3 TargetPosition;
    public bool IsTargetKilled;
    public Vector3 LookDirection;
}