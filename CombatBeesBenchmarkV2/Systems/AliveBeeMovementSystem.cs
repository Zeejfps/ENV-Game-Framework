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
    public Vector3 TargetVelocity;
    public bool IsTargetKilled;
}

public sealed class AliveBeeMovementSystem
{
    private ILogger Logger { get; }
    private readonly List<IAliveBee> m_Entities = new();
    private readonly AliveBeeState[] m_States;

    public AliveBeeMovementSystem(int maxStatCount, ILogger logger)
    {
        Logger = logger;
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
        var attackDistanceSqr = 2f * dt;
        var hitDistanceSqrd = 2f * dt;
        var chaseForce = 2f * dt;
        var attackForce = 2f * dt;

        var teamAttraction = 0f * dt;
        var teamRepulsion = 0f * dt;
        var flightJitter = 0f * dt;
        var damping = 0f * dt;
        
        var states = m_States.AsSpan();
        var stateCount = m_Entities.Count;
        //Logger.Trace($"State Count: {stateCount}");
        for (var i = 0; i < stateCount; i++)
            states[i] = m_Entities[i].Save();

        for (var i = 0; i < stateCount; i++)
        {
            ref var state = ref states[i];
            state.Velocity += state.MoveDirection * flightJitter;
            state.Velocity *= damping;
       
            var attractionPoint = state.AttractionPoint;
            Vector3 delta = attractionPoint - state.Position;
            var dist = MathF.Sqrt(delta.X * delta.X + delta.Y * delta.Y + delta.Z * delta.Z);
            if (dist > 0f)
                state.Velocity += delta * (teamAttraction / dist);

            var repellentPoint = state.RepellentPoint;
            delta = repellentPoint - state.Position;
            dist = MathF.Sqrt(delta.X * delta.X + delta.Y * delta.Y + delta.Z * delta.Z);
            if (dist > 0f)
                state.Velocity -= delta * (teamRepulsion / dist);
            
            delta = state.TargetPosition - state.Position;
            var sqrDist = delta.LengthSquared();
            if (sqrDist > attackDistanceSqr)
            {
                state.Velocity += delta * (chaseForce / MathF.Sqrt(sqrDist));
            }
            else
            {
                state.Velocity += delta * (attackForce / MathF.Sqrt(sqrDist));
                if (sqrDist < hitDistanceSqrd)
                {
                    state.TargetVelocity *= .5f;
                    state.IsTargetKilled = true;
                }
            }

            state.Position += state.Velocity * dt;
        }

        for (var i = 0; i < stateCount; i++)
        {
            m_Entities[i].Load(states[i]);
        }
    }
}