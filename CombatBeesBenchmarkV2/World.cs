using EasyGameFramework.Api;

namespace CombatBeesBenchmark;

public sealed class World
{
    public World(
        BeePool<IAliveBee> aliveBeePool,
        BeePool<IDeadBee> deadBeePool,
        AliveBeeMovementSystem aliveBeeMovementSystem,
        DeadBeeMovementSystem deadBeeMovementSystem,
        BeeRenderingSystem beeRenderingSystem, ILogger logger)
    {
        AliveBeePool = aliveBeePool;
        DeadBeePool = deadBeePool;
        AliveBeeMovementSystem = aliveBeeMovementSystem;
        DeadBeeMovementSystem = deadBeeMovementSystem;
        BeeRenderingSystem = beeRenderingSystem;
        Logger = logger;
    }
    
    private ILogger Logger { get; }
    private BeePool<IAliveBee> AliveBeePool { get; }
    private BeePool<IDeadBee> DeadBeePool { get; }
    private AliveBeeMovementSystem AliveBeeMovementSystem { get; }
    private DeadBeeMovementSystem DeadBeeMovementSystem { get; }
    private BeeRenderingSystem BeeRenderingSystem { get; }
    private Dictionary<IAliveBee, IAliveBee> BeeTargetTable { get; } = new();

    private HashSet<IAliveBee> BeesToKill { get; } = new();
    private HashSet<IDeadBee> BeesToSpawn { get; } = new();

    public void Spawn(IDeadBee deadBee)
    {
        BeesToSpawn.Add(deadBee);
    }

    public void Kill(IAliveBee aliveBee)
    {
        BeesToKill.Add(aliveBee);
    }

    public void Update(float dt)
    {
        if (BeesToKill.Count > 0)
            Logger.Trace($"Killing Bees: {BeesToKill.Count}");
        foreach (var aliveBee in BeesToKill)
        {
            AliveBeePool.Remove(aliveBee);
            AliveBeeMovementSystem.Remove(aliveBee);
            BeeRenderingSystem.Remove(aliveBee);

            var deadBee = new DeadBee(aliveBee.TeamIndex, this)
            {
                Position = aliveBee.Position,
                Velocity = aliveBee.Velocity * 0.5f,
                DeathTimer = 10f
            };
            DeadBeePool.Add(deadBee);
            DeadBeeMovementSystem.Add(deadBee);
            BeeRenderingSystem.Add(deadBee);
        }
        BeesToKill.Clear();

        if (BeesToSpawn.Count > 0)
            Logger.Trace($"Spawning Bees: {BeesToSpawn.Count}");
        foreach (var deadBee in BeesToSpawn)
        {
            DeadBeePool.Remove(deadBee);
            DeadBeeMovementSystem.Remove(deadBee);
            BeeRenderingSystem.Remove(deadBee);

            var aliveBee = new AliveBee(deadBee.TeamIndex, this)
            {
                
            };
            AliveBeePool.Add(aliveBee);
            AliveBeeMovementSystem.Add(aliveBee);
            BeeRenderingSystem.Add(aliveBee);
        }
        BeesToSpawn.Clear();
    }

    public IAliveBee GetTarget(AliveBee bee)
    {
        if (!BeeTargetTable.TryGetValue(bee, out var target))
        {
            target = AliveBeePool.GetRandomEnemyBee(bee.TeamIndex);
            BeeTargetTable[bee] = target;
        }
        return target;
    }
}