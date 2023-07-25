using System.Numerics;
using EasyGameFramework.Api;

namespace CombatBeesBenchmark;

public interface IDeadBee : IBee, IRenderableBee
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
    private ILogger Logger { get; }
    private IBeePool<IDeadBee> DeadBees { get; }
    private readonly DeadBeeState[] m_States;
    
    public DeadBeeMovementSystem(int maxBeeCount, IBeePool<IDeadBee> deadBees, ILogger logger)
    {
        Logger = logger;
        DeadBees = deadBees;
        m_States = new DeadBeeState[maxBeeCount];
    }

    public void Update(float dt)
    {
        var gravity = -20f * dt;
        var stateCount = DeadBees.Count;

        Parallel.For(0, stateCount, i =>
        {
            m_States[i] = DeadBees[i].Save();
        });

        var states = m_States.AsSpan();
        for (var i = 0; i < stateCount; i++)
        {
            ref var state = ref states[i];
            state.Velocity.Y += gravity;
            state.Position += state.Velocity * dt;
            state.DeathTimer -= dt;
        }
        
        Parallel.For(0, stateCount, i =>
        {
            DeadBees[i].Load(m_States[i]);
        });
    }
}