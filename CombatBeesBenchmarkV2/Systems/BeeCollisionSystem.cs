using System.Numerics;
using EasyGameFramework.Api;

namespace CombatBeesBenchmark;

public interface IMovableBee
{
    MovementState SaveMovementState();
    void Load(MovementState state);
}

public struct MovementState
{
    public Vector3 Position;
    public Vector3 Velocity;
}

public sealed class BeeCollisionSystem
{
    private ILogger Logger { get; }
    private readonly HashSet<IMovableBee> m_Entities = new();
    private readonly MovementState[] m_States;

    public BeeCollisionSystem(int maxBeeCount, ILogger logger)
    {
        Logger = logger;
        m_States = new MovementState[maxBeeCount];
    }

    public void Add(IMovableBee entity)
    {
        var added = m_Entities.Add(entity);
        // Logger.Trace($"Added: {entity.GetHashCode()}, Added?: {added}, Count: {m_Entities.Count}");
    }

    public void Remove(IMovableBee entity)
    {
        var removed = m_Entities.Remove(entity);
        // Logger.Trace($"Removed: {entity.GetHashCode()}, Removed?: {removed}, Count: {m_Entities.Count}");
    }
    
    public void Update(float dt)
    {
        var fieldHalfX = 100f * 0.5f;
        var fieldHalfY = 20f * 0.5f;
        var fieldHalfZ = 30f * 0.5f;
        
        var states = m_States.AsSpan();
        var stateCount = m_Entities.Count;
        //Logger.Trace($"State Count: {stateCount}");
        
        var index = 0;
        foreach (var entity in m_Entities)
        {
            states[index] = entity.SaveMovementState();
            index++;
        }

        for (var i = 0; i < stateCount; i++)
        {
            ref var state = ref states[i];
            
            if (MathF.Abs(state.Position.X) > fieldHalfX)
            {
                state.Position.X = fieldHalfX * MathF.Sign(state.Position.X);
                state.Velocity.X *= -.5f;
                state.Velocity.Y *= .8f;
                state.Velocity.Z *= .8f;
            }
            if (MathF.Abs(state.Position.Z) > fieldHalfZ)
            {
                state.Position.Z = fieldHalfZ * MathF.Sign(state.Position.Z);
                state.Velocity.Z *= -.5f;
                state.Velocity.X *= .8f;
                state.Velocity.Y *= .8f;
            }
            if (MathF.Abs(state.Position.Y) > fieldHalfY)
            {
                state.Position.Y = fieldHalfY * MathF.Sign(state.Position.Y);
                state.Velocity.Y *= -.5f;
                state.Velocity.Z *= .8f;
                state.Velocity.X *= .8f;
            }
        }
        
        index = 0;
        foreach (var entity in m_Entities)
        {
            entity.Load(states[index]);
            index++;
        }
    }
}