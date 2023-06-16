using System.Numerics;

namespace CombatBeesBenchmark;

public interface IDeadBee : IBee
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
    public DeadBeeMovementSystem(int maxBeeCount)
    {
        
    }

    public void Remove(IDeadBee bee)
    {
        throw new NotImplementedException();
    }

    public void Add(IDeadBee bee)
    {
        throw new NotImplementedException();
    }

    public void Update(float dt)
    {
        throw new NotImplementedException();
    }
}