using CombatBeesBenchmark;
using CombatBeesBenchmarkV2.Archetype;
using CombatBeesBenchmarkV2.EcsPrototype;

namespace CombatBeesBenchmarkV2.Systems;

public sealed class KillSystem : System<Bee, KilledArchetype>
{
    private readonly BeePool<Bee> m_AliveBees;
    
    public KillSystem(World<Bee> world, int size, BeePool<Bee> aliveBees) : base(world, size)
    {
        m_AliveBees = aliveBees;
    }

    protected override void OnUpdate(float dt, ref Memory<KilledArchetype> memory)
    {
        var components = memory.Span;
        for (var i = 0; i < components.Length; i++)
        {
            ref var archetype = ref components[i];
            m_AliveBees.Remove(archetype.In.Bee);
        }
    }
}