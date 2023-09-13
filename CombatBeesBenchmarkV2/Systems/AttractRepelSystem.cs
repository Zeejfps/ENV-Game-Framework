using CombatBeesBenchmark;
using CombatBeesBenchmarkV2.Archetype;
using CombatBeesBenchmarkV2.EcsPrototype;

namespace CombatBeesBenchmarkV2.Systems;

public sealed class AttractRepelSystem : System<AttractRepelArchetype>
{
    private BeePool<Bee> AliveBeePool { get; }
    private Random Random { get; }

    public AttractRepelSystem(IWorld world, int size, BeePool<Bee> aliveBeePool, Random random) : base(world, size)
    {
        AliveBeePool = aliveBeePool;
        Random = random;
    }

    protected override void OnUpdate(float dt, ref Span<AttractRepelArchetype> components)
    {
        for (var i = 0; i < components.Length; i++)
        {
            ref var component = ref components[i];
            component.AttractionPoint = AliveBeePool.GetRandomAllyBee(component.TeamIndex).Position;
            component.RepellentPoint = AliveBeePool.GetRandomAllyBee(component.TeamIndex).Position;
            component.MoveDirection = Random.RandomInsideUnitSphere();
        }
    }
}