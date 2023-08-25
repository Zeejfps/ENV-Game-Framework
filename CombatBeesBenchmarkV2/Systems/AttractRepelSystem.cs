using CombatBeesBenchmark;
using CombatBeesBenchmarkV2.Components;
using CombatBeesBenchmarkV2.EcsPrototype;

namespace CombatBeesBenchmarkV2.Systems;

public sealed class AttractRepelSystem : System<AttractRepelComponent>
{
    private BeePool<Bee> AliveBeePool { get; }

    public AttractRepelSystem(IWorld world, int size, BeePool<Bee> aliveBeePool) : base(world, size)
    {
        AliveBeePool = aliveBeePool;
    }

    protected override void OnUpdate(float dt, ref Span<AttractRepelComponent> components)
    {
        for (var i = 0; i < components.Length; i++)
        {
            ref var component = ref components[i];
            component.AttractionPoint = AliveBeePool.GetRandomAllyBee(component.TeamIndex).Position;
            component.RepellentPoint = AliveBeePool.GetRandomAllyBee(component.TeamIndex).Position;
        }
    }
}