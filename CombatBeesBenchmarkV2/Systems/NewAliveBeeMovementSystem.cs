using System.Numerics;
using CombatBeesBenchmarkV2.Archetype;
using CombatBeesBenchmarkV2.EcsPrototype;

namespace CombatBeesBenchmarkV2.Systems;

public sealed class NewAliveBeeMovementSystem : System<AliveBeeArchetype>
{
    public NewAliveBeeMovementSystem(IWorld world, int size) : base(world, size)
    {
    }

    protected override void OnUpdate(float dt, ref Span<AliveBeeArchetype> components)
    {
        var attackDistanceSqr = 4f * 4f;
        var hitDistanceSqrd = 0.5f * 0.5f;
        var chaseForce = 50f * dt;
        var attackForce = 500f * dt;

        var teamAttraction = 5f * dt;
        var teamRepulsion = 4f * dt;
        var flightJitter = 200f * dt;
        var damping = 1f - 0.9f * dt;
        
        var stateCount = components.Length;
        for (var i = 0; i < stateCount; i++)
        {
            ref var state = ref components[i];
            state.Movement.Velocity += state.MoveDirection * flightJitter;
            state.Movement.Velocity *= damping;
       
            var attractionPoint = state.AttractionPoint;
            Vector3 delta = attractionPoint - state.Movement.Position;
            var dist = MathF.Sqrt(delta.X * delta.X + delta.Y * delta.Y + delta.Z * delta.Z);
            if (dist > 0f)
                state.Movement.Velocity += delta * (teamAttraction / dist);
            
            var repellentPoint = state.RepellentPoint;
            delta = repellentPoint - state.Movement.Position;
            dist = MathF.Sqrt(delta.X * delta.X + delta.Y * delta.Y + delta.Z * delta.Z);
            if (dist > 0f)
                state.Movement.Velocity -= delta * (teamRepulsion / dist);
            
            delta = state.TargetPosition - state.Movement.Position;
            //Logger.Trace($"[{i}]: {state.TargetPosition}, Delta: {delta}");
            var sqrDist = delta.LengthSquared();
            if (sqrDist > attackDistanceSqr)
            {
                //Logger.Trace($"[{i}] Attacking!: {delta}");
                //Logger.Trace($"[{i}] Vel Before: {state.Velocity}");
                state.Movement.Velocity += delta * (chaseForce / MathF.Sqrt(sqrDist));
                //Logger.Trace($"[{i}] Vel After: {state.Velocity}");
            }
            else
            {
                state.Movement.Velocity += delta * (attackForce / MathF.Sqrt(sqrDist));
                if (sqrDist < hitDistanceSqrd)
                {
                    state.IsTargetKilled = true;
                }
            }

            //Logger.Trace($"[{i}] Velocity: {state.Velocity}");
            state.LookDirection = Vector3.Lerp(state.LookDirection, Vector3.Normalize(state.Movement.Velocity), dt * 4f);
            state.Movement.Position += state.Movement.Velocity * dt;
        }
    }
}