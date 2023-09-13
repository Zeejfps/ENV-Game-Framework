using System.Numerics;

namespace CombatBeesBenchmarkV2.Archetype;

public struct AttractRepelArchetype
{
    public int TeamIndex;
    public Vector3 AttractionPoint;
    public Vector3 RepellentPoint;
    public Vector3 MoveDirection;
}