using System.Numerics;
using CombatBeesBenchmarkV3.Archetypes;
using CombatBeesBenchmarkV3.EcsPrototype;

namespace CombatBeesBenchmarkV3.Systems;

public sealed class AliveBeeMovementSystem : System<Entity, AliveMovableBee>
{
    public AliveBeeMovementSystem(World<Entity> world, int size) : base(world, size)
    {
    }

    protected override void OnRead()
    {
        
    }

    protected override void OnUpdate(float dt, ref Span<AliveMovableBee> archetypes)
    {
        var attackDistanceSqr = 4f * 4f;
        var hitDistanceSqrd = 0.5f * 0.5f;
        var chaseForce = 50f * dt;
        var attackForce = 500f * dt;

        var teamAttraction = 5f * dt;
        var teamRepulsion = 4f * dt;
        var flightJitter = 200f * dt;
        var damping = 1f - 0.9f * dt;
        
        var stateCount = archetypes.Length;
        for (var i = 0; i < stateCount; i++)
        {
            ref var component = ref archetypes[i];
            component.Movement.Velocity += component.MoveDirection * flightJitter;
            component.Movement.Velocity *= damping;
       
            var attractionPoint = component.AttractionPoint;
            Vector3 delta = attractionPoint - component.Movement.Position;
            var dist = MathF.Sqrt(delta.X * delta.X + delta.Y * delta.Y + delta.Z * delta.Z);
            if (dist > 0f)
                component.Movement.Velocity += delta * (teamAttraction / dist);
            
            var repellentPoint = component.RepellentPoint;
            delta = repellentPoint - component.Movement.Position;
            dist = MathF.Sqrt(delta.X * delta.X + delta.Y * delta.Y + delta.Z * delta.Z);
            if (dist > 0f)
                component.Movement.Velocity -= delta * (teamRepulsion / dist);
            
            delta = component.TargetPosition - component.Movement.Position;
            //Logger.Trace($"[{i}]: {state.TargetPosition}, Delta: {delta}");
            var sqrDist = delta.LengthSquared();
            if (sqrDist > attackDistanceSqr)
            {
                //Logger.Trace($"[{i}] Attacking!: {delta}");
                //Logger.Trace($"[{i}] Vel Before: {state.Velocity}");
                component.Movement.Velocity += delta * (chaseForce / MathF.Sqrt(sqrDist));
                //Logger.Trace($"[{i}] Vel After: {state.Velocity}");
            }
            else
            {
                component.Movement.Velocity += delta * (attackForce / MathF.Sqrt(sqrDist));
                if (sqrDist < hitDistanceSqrd)
                {
                    component.IsTargetKilled = true;
                }
            }

            // Console.WriteLine($"[{i}] Velocity: {component.Movement.Velocity}");
            // Console.WriteLine($"[{i}] LookDirection: {component.LookDirection}");
            component.LookDirection = Vector3.Lerp(component.LookDirection, Vector3.Normalize(component.Movement.Velocity), dt * 4f);
            component.Movement.Position += component.Movement.Velocity * dt;
        }
    }

    protected override void OnWrite()
    {
    }
}