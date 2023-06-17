using System.Numerics;
using EasyGameFramework.Api;

namespace CombatBeesBenchmark;

public sealed class World
{
    public World(
        int numberOfTeams,
        int numberOfBeesPerTeam,
        BeePool<Bee> aliveBeePool,
        BeePool<Bee> deadBeePool,
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

        for (var teamIndex = 0; teamIndex < numberOfTeams; teamIndex++)
        {
            Logger.Trace($"Team Index: {teamIndex}");
            for (var j = 0; j < numberOfBeesPerTeam; j++)
            {
                Logger.Trace($"J: {j}");
                var bee = new Bee(teamIndex, this);
                BeeRenderingSystem.Add(bee);
                BeeCollisionSystem.Add(bee);
                Spawn(bee);
            }
        }
    }
    
    private Random Random { get; }
    private ILogger Logger { get; }
    private BeePool<Bee> AliveBeePool { get; }
    private BeePool<Bee> DeadBeePool { get; }
    private AliveBeeMovementSystem AliveBeeMovementSystem { get; }
    private DeadBeeMovementSystem DeadBeeMovementSystem { get; }
    private BeeRenderingSystem BeeRenderingSystem { get; }
    private BeeCollisionSystem BeeCollisionSystem { get; }
    private Dictionary<Bee, Bee> BeeTargetTable { get; } = new();

    private HashSet<Bee> BeesToKill { get; } = new();
    private HashSet<Bee> BeesToSpawn { get; } = new();

    public void Spawn(Bee deadBee)
    {
        if (deadBee.IsAlive)
            return;
        
        BeesToSpawn.Add(deadBee);
    }

    public void Kill(Bee aliveBee)
    {
        if (!aliveBee.IsAlive)
            return;
        
        BeesToKill.Add(aliveBee);
    }

    public void Update(float dt)
    {
        // if (BeesToKill.Count > 0)
        //     Logger.Trace($"Killing Bees: {BeesToKill.Count}");
        foreach (var bee in BeesToKill)
        {
            //Logger.Trace($"Killing Bee: {bee.GetHashCode()}");
            AliveBeePool.Remove(bee);
            AliveBeeMovementSystem.Remove(bee);
            BeeTargetTable.Remove(bee);

            bee.DeathTimer = 1f;
            bee.IsAlive = false;
            
            DeadBeePool.Add(bee);
            DeadBeeMovementSystem.Add(bee);
        }
        BeesToKill.Clear();

        // if (BeesToSpawn.Count > 0)
        //     Logger.Trace($"Spawning Bees: {BeesToSpawn.Count}");
        foreach (var bee in BeesToSpawn)
        {
            DeadBeePool.Remove(bee);
            DeadBeeMovementSystem.Remove(bee);
            
            var spawnPosition = Vector3.UnitX * (-100f * .4f + 100f * .8f * bee.TeamIndex);
            bee.Position = spawnPosition;
            bee.IsAlive = true;
            AliveBeePool.Add(bee);
            AliveBeeMovementSystem.Add(bee);
        }
        BeesToSpawn.Clear();
    }

    public Bee GetTarget(Bee bee)
    {
        if (!BeeTargetTable.TryGetValue(bee, out var target))
        {
            target = AliveBeePool.GetRandomEnemyBee(bee.TeamIndex);
            BeeTargetTable[bee] = target;
        }
        else if (!target.IsAlive)
        {
            target = AliveBeePool.GetRandomEnemyBee(bee.TeamIndex);
            BeeTargetTable[bee] = target;
        }
        return target;
    }

    public void AssignNewTarget(Bee bee)
    {
        var target = AliveBeePool.GetRandomEnemyBee(bee.TeamIndex);
        BeeTargetTable[bee] = target;
    }
}