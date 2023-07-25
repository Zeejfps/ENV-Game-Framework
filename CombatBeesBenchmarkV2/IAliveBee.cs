using System.Numerics;

namespace CombatBeesBenchmark;

public interface IAliveBee : IBee, IRenderableBee
{
    AliveBeeState Save();
    void Load(AliveBeeState state);
}