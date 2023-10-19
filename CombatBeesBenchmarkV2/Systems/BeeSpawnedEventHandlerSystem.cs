using CombatBeesBenchmark;
using CombatBeesBenchmarkV2.Archetype;
using CombatBeesBenchmarkV2.EcsPrototype;

namespace CombatBeesBenchmarkV2.Systems;

public sealed class BeeSpawnedEventHandlerSystem : System<BeeSpawnedEvent>
{
    private BeePool<Bee> m_AliveBees;
    
    public BeeSpawnedEventHandlerSystem(IWorld world, int size, BeePool<Bee> aliveBees) : base(world, size)
    {
        m_AliveBees = aliveBees;
    }

    protected override void OnUpdate(float dt, ref Span<BeeSpawnedEvent> events)
    {
        for (var i = 0; i < events.Length; i++)
        {
            ref var e = ref events[i];
            m_AliveBees.Add(e.Bee);
        }
        
        for (var i = 0; i < events.Length; i++)
        {
            ref var e = ref events[i];
            World.Remove<BeeSpawnedEvent>(e.Bee);
        }
    }
}