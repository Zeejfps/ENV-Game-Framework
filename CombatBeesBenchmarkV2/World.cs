namespace CombatBeesBenchmark;

public sealed class World
{
    public World(
        BeePool<IAliveBee> aliveBeePool,
        BeePool<IDeadBee> deadBeePool,
        AliveBeeMovementSystem aliveBeeMovementSystem,
        DeadBeeMovementSystem deadBeeMovementSystem)
    {
        AliveBeePool = aliveBeePool;
        DeadBeePool = deadBeePool;
        AliveBeeMovementSystem = aliveBeeMovementSystem;
        DeadBeeMovementSystem = deadBeeMovementSystem;
    }

    private BeePool<IAliveBee> AliveBeePool { get; }
    private BeePool<IDeadBee> DeadBeePool { get; }
    private AliveBeeMovementSystem AliveBeeMovementSystem { get; }
    private DeadBeeMovementSystem DeadBeeMovementSystem { get; }

    public IAliveBee GetRandomEnemyBee(int teamIndex)
    {
        return AliveBeePool.GetRandomEnemyBee(teamIndex);
    }

    public void Kill(IAliveBee aliveBee)
    {
        AliveBeePool.Remove(aliveBee);
        AliveBeeMovementSystem.Remove(aliveBee);
        
        var deadBee = new DeadBee(aliveBee.TeamIndex);
        DeadBeePool.Add(deadBee);
        DeadBeeMovementSystem.Add(deadBee);
    }

    public void Spawn(IDeadBee bee)
    {
        DeadBeePool.Remove(bee);
        DeadBeeMovementSystem.Remove(bee);

        var aliveBee = new AliveBee(bee.TeamIndex, this);
        AliveBeePool.Add(aliveBee);
        AliveBeeMovementSystem.Add(aliveBee);
    }
}