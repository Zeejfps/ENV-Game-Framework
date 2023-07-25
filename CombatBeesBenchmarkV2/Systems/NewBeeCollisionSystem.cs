using CombatBeesBenchmarkV2.EcsPrototype;

namespace CombatBeesBenchmark;

public sealed class NewBeeCollisionSystem : System<CollisionComponent>
{
    public NewBeeCollisionSystem(IWorld world, int size) : base(world, size)
    {
    }

    protected override void OnUpdate(float dt, ref Span<CollisionComponent> components)
    {
        var fieldHalfX = 100f * 0.5f;
        var fieldHalfY = 20f * 0.5f;
        var fieldHalfZ = 30f * 0.5f;
        
        var componentsCount = components.Length;
        for (var i = 0; i < componentsCount; i++)
        {
            ref var component = ref components[i];
            ref var movement = ref component.MovementState;

            if (MathF.Abs(movement.Position.X) > fieldHalfX)
            {
                movement.Position.X = fieldHalfX * MathF.Sign(movement.Position.X);
                movement.Velocity.X *= -.5f;
                movement.Velocity.Y *= .8f;
                movement.Velocity.Z *= .8f;
            }
            if (MathF.Abs(movement.Position.Z) > fieldHalfZ)
            {
                movement.Position.Z = fieldHalfZ * MathF.Sign(movement.Position.Z);
                movement.Velocity.Z *= -.5f;
                movement.Velocity.X *= .8f;
                movement.Velocity.Y *= .8f;
            }
            if (MathF.Abs(movement.Position.Y) > fieldHalfY)
            {
                movement.Position.Y = fieldHalfY * MathF.Sign(movement.Position.Y);
                movement.Velocity.Y *= -.5f;
                movement.Velocity.Z *= .8f;
                movement.Velocity.X *= .8f;
            }
        }
    }
}