using System.Numerics;
using EasyGameFramework.Api;

namespace CombatBeesBenchmark;

public struct AliveBeeState
{
    public Vector3 Position;
    public Vector3 Velocity;
    public Vector3 MoveDirection;
    public Vector3 AttractionPoint;
    public Vector3 RepellentPoint;
    public Vector3 TargetPosition;
    public bool IsTargetKilled;
}

public sealed class AliveBeeMovementSystem
{
    private ILogger Logger { get; }
    private Random Random { get; }
    private readonly List<IAliveBee> m_Entities = new();
    private readonly AliveBeeState[] m_States;

    public AliveBeeMovementSystem(int maxStatCount, ILogger logger, Random random)
    {
        Logger = logger;
        Random = random;
        m_States = new AliveBeeState[maxStatCount];
    }

    public void Add(IAliveBee bee)
    {
        m_Entities.Add(bee);
    }

    public void Remove(IAliveBee bee)
    {
        m_Entities.Remove(bee);
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
        
        var states = m_States.AsSpan();
        var stateCount = m_Entities.Count;
        //Logger.Trace($"State Count: {stateCount}");
        for (var i = 0; i < stateCount; i++)
        {
            var state = m_Entities[i].Save();
            state.MoveDirection = Random.RandomInsideUnitSphere();
            //Logger.Trace($"[{i}] Move Dir: {state.MoveDirection}");
            states[i] = state;
        }

        for (var i = 0; i < stateCount; i++)
        {
            ref var state = ref states[i];
            state.Velocity += state.MoveDirection * flightJitter;
            state.Velocity *= damping;
       
            // var attractionPoint = state.AttractionPoint;
            // Vector3 delta = attractionPoint - state.Position;
            // var dist = MathF.Sqrt(delta.X * delta.X + delta.Y * delta.Y + delta.Z * delta.Z);
            // if (dist > 0f)
            //     state.Velocity += delta * (teamAttraction / dist);
            //
            // var repellentPoint = state.RepellentPoint;
            // delta = repellentPoint - state.Position;
            // dist = MathF.Sqrt(delta.X * delta.X + delta.Y * delta.Y + delta.Z * delta.Z);
            // if (dist > 0f)
            //     state.Velocity -= delta * (teamRepulsion / dist);
            //
            var delta = state.TargetPosition - state.Position;
            //Logger.Trace($"[{i}]: {state.TargetPosition}, Delta: {delta}");
            var sqrDist = delta.LengthSquared();
            if (sqrDist > attackDistanceSqr)
            {
                //Logger.Trace($"[{i}] Attacking!: {delta}");
                //Logger.Trace($"[{i}] Vel Before: {state.Velocity}");
                state.Velocity += delta * (chaseForce / MathF.Sqrt(sqrDist));
                //Logger.Trace($"[{i}] Vel After: {state.Velocity}");
            }
            else
            {
                state.Velocity += delta * (attackForce / MathF.Sqrt(sqrDist));
                if (sqrDist < hitDistanceSqrd)
                {
                    state.IsTargetKilled = true;
                }
            }

            //Logger.Trace($"[{i}] Velocity: {state.Velocity}");
            state.Position += state.Velocity * dt;
        }

        for (var i = 0; i < stateCount; i++)
        {
            m_Entities[i].Load(states[i]);
        }
    }
}