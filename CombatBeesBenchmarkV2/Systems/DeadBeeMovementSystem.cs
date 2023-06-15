namespace CombatBeesBenchmark;

public interface IDeadBee
{
    DeadBeeState Save();
    void Load(DeadBeeState state);
}

public struct DeadBeeState
{
    public BeeState Bee;
    public float DeathTimer;
}

public sealed class DeadBeeMovementSystem
{
    public void Remove(IDeadBee bee)
    {
        throw new NotImplementedException();
    }

    public void Add(IDeadBee bee)
    {
        throw new NotImplementedException();
    }
}