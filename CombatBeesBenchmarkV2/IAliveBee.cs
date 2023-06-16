using System.Numerics;

namespace CombatBeesBenchmark;

public interface IAliveBee : IBee
{
    Vector3 Position { get; set; }
    Vector3 Velocity { get; set; }
    AliveBeeState Save();
    void Load(AliveBeeState state);
}