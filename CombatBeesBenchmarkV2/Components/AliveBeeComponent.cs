using System.Numerics;

namespace CombatBeesBenchmarkV2.Components;

public struct AliveBeeComponent
{
    public MovementComponent Movement;
    public Vector3 MoveDirection;
    public Vector3 AttractionPoint;
    public Vector3 RepellentPoint;
    public Vector3 TargetPosition;
    public bool IsTargetKilled;
    public Vector3 LookDirection;
    public int TeamIndex;
}