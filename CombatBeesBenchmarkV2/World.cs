using System.Collections;
using System.Collections.Concurrent;
using System.Numerics;
using System.Runtime.InteropServices;
using CombatBeesBenchmarkV2.Components;
using CombatBeesBenchmarkV2.EcsPrototype;
using EasyGameFramework.Api;

namespace CombatBeesBenchmark;

public sealed class World : IWorld
{
    public World(
        int numberOfTeams,
        int numberOfBeesPerTeam,
        BeePool<Bee> aliveBeePool,
        ILogger logger, Random random)
    {
        AliveBeePool = aliveBeePool;
        Logger = logger;
        Random = random;

        for (var teamIndex = 0; teamIndex < numberOfTeams; teamIndex++)
        {
            //Logger.Trace($"Team Index: {teamIndex}");
            for (var j = 0; j < numberOfBeesPerTeam; j++)
            {
                //Logger.Trace($"J: {j}");
                var bee = new Bee(teamIndex, this, random, aliveBeePool);
                Add<BeeRenderComponent>(bee);
                Add<CollisionComponent>(bee);
                Spawn(bee);
            }
        }
    }
    
    private Random Random { get; }
    private ILogger Logger { get; }
    private BeePool<Bee> AliveBeePool { get; }
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
            Remove<AliveBeeComponent>(bee);

            bee.Velocity *= 0.5f;
            bee.DeathTimer = 5f;
            bee.IsAlive = false;
            
            Add<DeadBeeComponent>(bee);
        }
        BeesToKill.Clear();

        // if (BeesToSpawn.Count > 0)
        //     Logger.Trace($"Spawning Bees: {BeesToSpawn.Count}");
        foreach (var bee in BeesToSpawn)
        {
            Remove<DeadBeeComponent>(bee);
            var spawnPosition = Vector3.UnitX * (-100f * .4f + 100f * .8f * bee.TeamIndex);
            bee.Position = spawnPosition;
            bee.Size = Random.NextSingleInRange(0.25f, 0.5f);
            bee.IsAlive = true;
            AliveBeePool.Add(bee);
            Add<AliveBeeComponent>(bee);
        }
        BeesToSpawn.Clear();
    }

    public Bee GetRandomEnemy(int teamIndex)
    {
        return AliveBeePool.GetRandomEnemyBee(teamIndex);
    }

    private readonly ConcurrentDictionary<Type, IList> m_ComponentTypeToEntitiesTable = new();

    public int Query<TComponent>(IEntity<TComponent>[] buffer) where TComponent : struct
    {
        var componentType = typeof(TComponent);
        if (m_ComponentTypeToEntitiesTable.TryGetValue(componentType, out var entities))
        {
            var entitiesList = (List<IEntity<TComponent>>)entities;
            var entityCount = entitiesList.Count;
            lock (entitiesList)
            {
                var bufferSpan = buffer.AsSpan();
                var entitiesSpan = CollectionsMarshal.AsSpan(entitiesList);
                entitiesSpan.TryCopyTo(bufferSpan);
            }
            return entityCount;
        }
        return 0;
    }

    public void Add<TComponent>(IEntity<TComponent> entity) where TComponent : struct
    {
        var componentType = typeof(TComponent);
        if (!m_ComponentTypeToEntitiesTable.TryGetValue(componentType, out var entities))
        {
            entities = new List<IEntity<TComponent>>();
            m_ComponentTypeToEntitiesTable[componentType] = entities;
        }
        lock (entities)
            entities.Add(entity);
    }

    public void Remove<TComponent>(IEntity<TComponent> entity) where TComponent : struct
    {
        var componentType = typeof(TComponent);
        if (m_ComponentTypeToEntitiesTable.TryGetValue(componentType, out var entities))
        {
            lock (entities)
                entities.Remove(entity);
        }
    }
}