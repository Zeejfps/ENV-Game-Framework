using CombatBeesBenchmarkV2.Components;
using CombatBeesBenchmarkV2.EcsPrototype;

namespace CombatBeesBenchmark;

public sealed class NewDeadBeeMovementSystem : System<DeadBeeComponent>
{
    public NewDeadBeeMovementSystem(IWorld world, int size) : base(world, size)
    {
    }

    protected override void OnUpdate(float dt, ref Span<DeadBeeComponent> components)
    {
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