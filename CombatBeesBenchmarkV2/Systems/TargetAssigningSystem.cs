using CombatBeesBenchmark;
using CombatBeesBenchmarkV2.Archetype;
using CombatBeesBenchmarkV2.EcsPrototype;

namespace CombatBeesBenchmarkV2.Systems;

public sealed class TargetAssigningSystem : System<NeedTargetArchetype>
{
    private readonly BeePool<Bee> m_AliveBees;
    
    public TargetAssigningSystem(IWorld world, int size, BeePool<Bee> aliveBees) : base(world, size)
    {
        m_AliveBees = aliveBees;
    }

    protected override void OnUpdate(float dt, ref Span<NeedTargetArchetype> archetypes)
    {
        for (var i = 0; i < archetypes.Length; i++)
        {
            ref var archetype = ref archetypes[i];
            archetype.Out.Target = m_AliveBees.GetRandomEnemyBee(archetype.In.TeamIndex);
        }
    }
}