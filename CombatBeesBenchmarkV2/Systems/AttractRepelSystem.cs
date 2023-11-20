using CombatBeesBenchmark;
using CombatBeesBenchmarkV2.Archetype;
using CombatBeesBenchmarkV2.EcsPrototype;

namespace CombatBeesBenchmarkV2.Systems;

public sealed class AttractRepelSystem : System<Bee, AttractRepelArchetype>
{
    private BeePool<Bee?> AliveBeePool { get; }
    private Random Random { get; }

    public AttractRepelSystem(World<Bee> world, int size, BeePool<Bee?> aliveBeePool, Random random) : base(world, size)
    {
        AliveBeePool = aliveBeePool;
        Random = random;
    }

    protected override void OnUpdate(float dt, ref Memory<AttractRepelArchetype> memory)
    {
        var components = memory.Span;
        for (var i = 0; i < components.Length; i++)
        {
            ref var component = ref components[i];
            
            var allyBee = AliveBeePool.GetRandomAllyBee(component.TeamIndex);
            if (allyBee != null) component.AttractionPoint = allyBee.Position;
            
            allyBee = AliveBeePool.GetRandomAllyBee(component.TeamIndex);
            if (allyBee != null) component.RepellentPoint = allyBee.Position;
            
            component.MoveDirection = Random.RandomInsideUnitSphere();
        }
    }
}