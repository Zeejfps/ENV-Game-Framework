using System.Numerics;
using CombatBeesBenchmarkV2.Components;
using EasyGameFramework.Api;

namespace CombatBeesBenchmark;

public sealed class AliveBeeMovementSystem
{
    private ILogger Logger { get; }
    private Random Random { get; }
    private readonly IBeePool<IAliveBee> m_Entities;
    private readonly AliveBeeComponent[] m_States;

    public AliveBeeMovementSystem(int maxStatCount, IBeePool<IAliveBee> entities, ILogger logger, Random random)
    {
        Logger = logger;
        Random = random;
        m_Entities = entities;
        m_States = new AliveBeeComponent[maxStatCount];
    }

    public void Update(float dt)
    {
        var attackDistanceSqr = 4f * 4f;
        var hitDistanceSqrd = 0.5f * 0.5f;
        var chaseForce = 50f * dt;
        var attackForce = 500f * dt;

        var teamAttraction = 5f * dt;
        var teamRepulsion = 4f * dt;
        var flightJitter = 200f * dt;
        var damping = 1f - 0.9f * dt;
        
        var stateCount = m_Entities.Count;
        //Logger.Trace($"State Count: {stateCount}");

        Parallel.For(0, stateCount, i =>
        {
            var entity = m_Entities[i];
            var state = entity.Save();
            state.MoveDirection = Random.RandomInsideUnitSphere();
            state.AttractionPoint = m_Entities.GetRandomAllyBee(entity.TeamIndex).Position;
            state.RepellentPoint = m_Entities.GetRandomAllyBee(entity.TeamIndex).Position;
            //Logger.Trace($"[{i}] Move Dir: {state.MoveDirection}");
            m_States[i] = state;
        });
        
        var states = m_States.AsSpan();
        for (var i = 0; i < stateCount; i++)
        {
            ref var state = ref states[i];
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

        Parallel.For(0, stateCount, (i) =>
        {
            m_Entities[i].Load(m_States[i]);
        });
    }
}