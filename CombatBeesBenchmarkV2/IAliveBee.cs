using System.Numerics;
using CombatBeesBenchmarkV2.Components;

namespace CombatBeesBenchmark;

public interface IAliveBee : IBee, IRenderableBee
{
    AliveBeeComponent Save();
    void Load(AliveBeeComponent state);
}