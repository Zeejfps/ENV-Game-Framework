using System.Numerics;

namespace CombatBeesBenchmark;

public interface IAliveBee : IBee, IRenderableBee, IMovableBee
{
    AliveBeeState Save();
    void Load(AliveBeeState state);
}