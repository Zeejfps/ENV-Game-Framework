using System.Numerics;
using CombatBeesBenchmark;

namespace CombatBeesBenchmarkV2.Archetype;

public struct SpawnableBeeArchetype
{
    public ReadOnlyData In;
    public WriteOnlyData Out;
    
    public struct ReadOnlyData
    {
        public int TeamIndex;
        public Bee Bee;
    }
    
    public struct WriteOnlyData
    {
        public Vector3 SpawnPosition;
        public float Size;
    }
}