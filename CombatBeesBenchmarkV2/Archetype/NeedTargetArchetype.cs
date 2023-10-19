using CombatBeesBenchmark;

namespace CombatBeesBenchmarkV2.Archetype;

public struct NeedTargetArchetype
{
    public ReadOnlyData In;
    public WriteOnlyData Out;

    public struct ReadOnlyData
    {
        public int TeamIndex;
    }
        
    public struct WriteOnlyData
    {
        public Bee? Target;
    }
}