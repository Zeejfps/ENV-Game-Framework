using System.Numerics;

namespace CombatBeesBenchmark;

public interface IAliveBee : IBee, IRenderableBee, IMovableBee
{
    Vector3 Position { get; }
    AliveBeeState Save();
    void Load(AliveBeeState state);
}