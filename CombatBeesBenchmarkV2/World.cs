using System.Numerics;
using EasyGameFramework.Api;

namespace CombatBeesBenchmark;

public sealed class World
{
    public World(
        BeePool<IAliveBee> aliveBeePool,
        BeePool<IDeadBee> deadBeePool,
        AliveBeeMovementSystem aliveBeeMovementSystem,
        DeadBeeMovementSystem deadBeeMovementSystem,
        BeeCollisionSystem beeCollisionSystem,
        BeeRenderingSystem beeRenderingSystem, ILogger logger, Random random)
    {
        AliveBeePool = aliveBeePool;
        DeadBeePool = deadBeePool;
        AliveBeeMovementSystem = aliveBeeMovementSystem;
        DeadBeeMovementSystem = deadBeeMovementSystem;
        BeeRenderingSystem = beeRenderingSystem;
        BeeCollisionSystem = beeCollisionSystem;
        Logger = logger;
        Random = random;
    }
    
    private Random Random { get; }
    private ILogger Logger { get; }
    private BeePool<IAliveBee> AliveBeePool { get; }
    private BeePool<IDeadBee> DeadBeePool { get; }
    private AliveBeeMovementSystem AliveBeeMovementSystem { get; }
    private DeadBeeMovementSystem DeadBeeMovementSystem { get; }
    private BeeRenderingSystem BeeRenderingSystem { get; }
    private BeeCollisionSystem BeeCollisionSystem { get; }
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
            BeeCollisionSystem.Remove(aliveBee);
            BeeTargetTable.Remove(aliveBee);

            var deadBee = new DeadBee(aliveBee.TeamIndex, this)
            {
                Position = aliveBee.Position,
                Velocity = aliveBee.Velocity,
                DeathTimer = 10f
            };
            DeadBeePool.Add(deadBee);
            DeadBeeMovementSystem.Add(deadBee);
            BeeRenderingSystem.Add(deadBee);
            BeeCollisionSystem.Add(deadBee);
        }
        BeesToKill.Clear();

        if (BeesToSpawn.Count > 0)
            Logger.Trace($"Spawning Bees: {BeesToSpawn.Count}");
        foreach (var deadBee in BeesToSpawn)
        {
            DeadBeePool.Remove(deadBee);
            DeadBeeMovementSystem.Remove(deadBee);
            BeeRenderingSystem.Remove(deadBee);
            BeeCollisionSystem.Remove(deadBee);
            
            var spawnPosition = Vector3.UnitX * (-100f * .4f + 100f * .8f * deadBee.TeamIndex);
            var aliveBee = new AliveBee(deadBee.TeamIndex, this)
            {
                Position = spawnPosition
            };
            AliveBeePool.Add(aliveBee);
            AliveBeeMovementSystem.Add(aliveBee);
            BeeRenderingSystem.Add(aliveBee);
            BeeCollisionSystem.Add(aliveBee);
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

    public void AssignNewTarget(AliveBee bee)
    {
        var target = AliveBeePool.GetRandomEnemyBee(bee.TeamIndex);
        BeeTargetTable[bee] = target;
    }
}