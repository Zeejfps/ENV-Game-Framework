using System.Numerics;

namespace CombatBeesBenchmarkV2.Archetype;

public struct SpawnableBeeArchetype
{
    public ReadOnlyData In;
    public WriteOnlyData Out;
    
    public struct ReadOnlyData
    {
        public int TeamIndex;
    }
    
    public struct WriteOnlyData
    {
        public Vector3 SpawnPosition;
        public float Size;
    }
}