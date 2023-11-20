using CombatBeesBenchmark;
using CombatBeesBenchmarkV2.Archetype;
using CombatBeesBenchmarkV2.EcsPrototype;

namespace CombatBeesBenchmarkV2.Systems;

public sealed class NewDeadBeeMovementSystem : System<Bee, DeadBeeArchetype>
{
    public NewDeadBeeMovementSystem(World<Bee> world, int size) : base(world, size)
    {
    }

    protected override void OnUpdate(float dt, ref Memory<DeadBeeArchetype> memory)
    {
        var components = memory.Span;
        var gravity = -20f * dt;
        var stateCount = components.Length;
        for (var i = 0; i < stateCount; i++)
        {
            ref var state = ref components[i];
            state.Movement.Velocity.Y += gravity;
            state.Movement.Position += state.Movement.Velocity * dt;
            state.DeathTimer -= dt;
        }
    }
}