using CombatBeesBenchmark;
using CombatBeesBenchmarkV2.Archetype;
using CombatBeesBenchmarkV2.EcsPrototype;

namespace CombatBeesBenchmarkV2.Systems;

public sealed class TargetAssigningSystem : System<Bee, NeedTargetArchetype>
{
    private readonly BeePool<Bee> m_AliveBees;
    
    public TargetAssigningSystem(World<Bee> world, int size, BeePool<Bee> aliveBees) : base(world, size)
    {
        m_AliveBees = aliveBees;
    }

    protected override void OnUpdate(float dt, ref Memory<NeedTargetArchetype> memory)
    {
        var archetypes = memory.Span;
        for (var i = 0; i < archetypes.Length; i++)
        {
            ref var archetype = ref archetypes[i];
            archetype.Out.Target = m_AliveBees.GetRandomEnemyBee(archetype.In.TeamIndex);
        }
    }
}