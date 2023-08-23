using System.Numerics;

namespace CombatBeesBenchmarkV2.Components;

public struct NeedsAttractRepelPositionsComponent
{
    public int TeamIndex;
    public Vector3 AttractionPoint;
    public Vector3 RepellentPoint;
}