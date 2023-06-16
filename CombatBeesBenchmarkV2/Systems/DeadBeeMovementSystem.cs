using System.Numerics;

namespace CombatBeesBenchmark;

public interface IDeadBee : IBee, IRenderableBee, IMovableBee
{
    DeadBeeState Save();
    void Load(DeadBeeState state);
}

public struct DeadBeeState
{
    public Vector3 Position;
    public Vector3 Velocity;
    public float DeathTimer;
}

public sealed class DeadBeeMovementSystem
{
    private readonly LinkedList<IDeadBee> m_Entities = new();
    private readonly DeadBeeState[] m_States;
    
    public DeadBeeMovementSystem(int maxBeeCount)
    {
        m_States = new DeadBeeState[maxBeeCount];
    }

    public void Remove(IDeadBee bee)
    {
        m_Entities.Remove(bee);
    }

    public void Add(IDeadBee bee)
    {
        m_Entities.AddLast(bee);
    }

    public void Update(float dt)
    {
        var gravity = -20f * dt;
        var states = m_States;
        var stateCount = m_Entities.Count;

        var index = 0;
        foreach (var entity in m_Entities)
        {
            states[index] = entity.Save();
            index++;
        }

        for (var i = 0; i < stateCount; i++)
        {
            ref var state = ref states[i];
            state.Velocity.Y += gravity;
            state.Position += state.Velocity * dt;
            state.DeathTimer -= dt;
        }
        
        index = 0;
        foreach (var entity in m_Entities)
        {
            entity.Load(states[index]);
            index++;
        }
    }
}