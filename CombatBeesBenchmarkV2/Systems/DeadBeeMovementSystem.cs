using CombatBeesBenchmarkV2.Components;
using EasyGameFramework.Api;

namespace CombatBeesBenchmark;

public interface IDeadBee : IBee
{
    DeadBeeComponent Save();
    void Load(DeadBeeComponent state);
}

public sealed class DeadBeeMovementSystem
{
    private ILogger Logger { get; }
    private IBeePool<Bee> DeadBees { get; }
    private readonly DeadBeeComponent[] m_States;
    
    public DeadBeeMovementSystem(int maxBeeCount, IBeePool<Bee> deadBees, ILogger logger)
    {
        Logger = logger;
        DeadBees = deadBees;
        m_States = new DeadBeeComponent[maxBeeCount];
    }

    public void Update(float dt)
    {
        var gravity = -20f * dt;
        var stateCount = DeadBees.Count;

        Parallel.For(0, stateCount, i =>
        {
            m_States[i] = ((IDeadBee)DeadBees[i]).Save();
        });

        var states = m_States.AsSpan();
        for (var i = 0; i < stateCount; i++)
        {
            ref var state = ref states[i];
            state.Movement.Velocity.Y += gravity;
            state.Movement.Position += state.Movement.Velocity * dt;
            state.DeathTimer -= dt;
        }
        
        Parallel.For(0, stateCount, i =>
        {
            DeadBees[i].Load(m_States[i]);
        });
    }
}