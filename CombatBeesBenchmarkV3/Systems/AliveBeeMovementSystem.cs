using System.Numerics;
using CombatBeesBenchmarkV3.Archetypes;
using CombatBeesBenchmarkV3.EcsPrototype;

namespace CombatBeesBenchmarkV3.Systems;

public sealed class AliveBeeMovementSystem : System<Entity, AliveBee>
{
        
    public AliveBeeMovementSystem(World<Entity> world, int size) : base(world, size)
    {
    }

    protected override void OnUpdate(float dt, ref Span<AliveBee> archetypes)
    {
        var attackDistanceSqr = 4f * 4f;
        var hitDistanceSqrd = 0.5f * 0.5f;
        var chaseForce = 50f * dt;
        var attackForce = 500f * dt;

        var teamAttraction = 5f * dt;
        var teamRepulsion = 4f * dt;
        var flightJitter = 200f * dt;
        var damping = 1f - 0.9f * dt;
        
        var archetypesLength = archetypes.Length;
        
        for (var i = 0; i < archetypesLength; i++)
        {
            ref var archetype = ref archetypes[i];
            archetype.Movement.Velocity += archetype.MoveDirection * flightJitter;
            archetype.Movement.Velocity *= damping;
       
            var attractionPoint = archetype.AttractionPoint;
            Vector3 delta = attractionPoint - archetype.Movement.Position;
            var dist = MathF.Sqrt(delta.X * delta.X + delta.Y * delta.Y + delta.Z * delta.Z);
            if (dist > 0f)
                archetype.Movement.Velocity += delta * (teamAttraction / dist);
            
            var repellentPoint = archetype.RepellentPoint;
            delta = repellentPoint - archetype.Movement.Position;
            dist = MathF.Sqrt(delta.X * delta.X + delta.Y * delta.Y + delta.Z * delta.Z);
            if (dist > 0f)
                archetype.Movement.Velocity -= delta * (teamRepulsion / dist);
            
            delta = archetype.TargetPosition - archetype.Movement.Position;
            //Logger.Trace($"[{i}]: {state.TargetPosition}, Delta: {delta}");
            var sqrDist = delta.LengthSquared();
            if (sqrDist > attackDistanceSqr)
            {
                //Logger.Trace($"[{i}] Attacking!: {delta}");
                //Logger.Trace($"[{i}] Vel Before: {state.Velocity}");
                archetype.Movement.Velocity += delta * (chaseForce / MathF.Sqrt(sqrDist));
                //Logger.Trace($"[{i}] Vel After: {state.Velocity}");
            }
            else
            {
                archetype.Movement.Velocity += delta * (attackForce / MathF.Sqrt(sqrDist));
                if (sqrDist < hitDistanceSqrd)
                {
                    archetype.IsTargetKilled = true;
                }
            }

            // Console.WriteLine($"[{i}] Velocity: {component.Movement.Velocity}");
            // Console.WriteLine($"[{i}] LookDirection: {component.LookDirection}");
            archetype.LookDirection = Vector3.Lerp(archetype.LookDirection, Vector3.Normalize(archetype.Movement.Velocity), dt * 4f);
            archetype.Movement.Position += archetype.Movement.Velocity * dt;
        }
    }
}