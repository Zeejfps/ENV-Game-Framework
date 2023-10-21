using System.Numerics;

namespace CombatBeesBenchmarkV3.Archetypes;

public struct SpawnableBee
{
    public ReadOnlyData In;
    public WriteOnlyData Out;
    
    public struct ReadOnlyData
    {
        public int TeamIndex;
        public Entity Bee;
    }
    
    public struct WriteOnlyData
    {
        public Vector3 SpawnPosition;
        public float Size;
    }
}