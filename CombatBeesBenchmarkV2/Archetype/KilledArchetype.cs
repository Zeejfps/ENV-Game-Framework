using CombatBeesBenchmark;

namespace CombatBeesBenchmarkV2.Archetype;

public struct KilledArchetype
{
    public ReadOnlyData In;
    
    public struct ReadOnlyData
    {
        public Bee Bee;
    }
}