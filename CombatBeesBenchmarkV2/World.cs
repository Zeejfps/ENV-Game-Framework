using System.Collections;
using System.Collections.Concurrent;
using System.Numerics;
using System.Runtime.InteropServices;
using CombatBeesBenchmarkV2.Archetype;
using CombatBeesBenchmarkV2.EcsPrototype;
using EasyGameFramework.Api;

namespace CombatBeesBenchmark;

public sealed class World : IWorld
{
    public World(
        BeePool<Bee> aliveBeePool,
        ILogger logger, Random random)
    {
        AliveBeePool = aliveBeePool;
        Logger = logger;
        Random = random;
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
        foreach (var bee in BeesToKill)
        {
            //Logger.Trace($"Killing Bee: {bee.GetHashCode()}");
            AliveBeePool.Remove(bee);
            Remove<AliveBeeArchetype>(bee);
        
            bee.Velocity *= 0.5f;
            bee.DeathTimer = 5f;
            bee.IsAlive = false;
            
            Add<DeadBeeArchetype>(bee);
            Remove<AttractRepelArchetype>(bee);
        }
        BeesToKill.Clear();
        
        foreach (var bee in BeesToSpawn)
        {
            Remove<DeadBeeArchetype>(bee);
            var spawnPosition = Vector3.UnitX * (-100f * .4f + 100f * .8f * bee.TeamIndex);
            bee.Position = spawnPosition;
            bee.Size = Random.NextSingleInRange(0.25f, 0.5f);
            bee.IsAlive = true;
            AliveBeePool.Add(bee);
            Add<AliveBeeArchetype>(bee);
            Add<AttractRepelArchetype>(bee);
        }
        BeesToSpawn.Clear();
    }

    public Bee GetRandomEnemy(int teamIndex)
    {
        return AliveBeePool.GetRandomEnemyBee(teamIndex);
    }

    private bool m_IsInFrame;
    private readonly ConcurrentDictionary<IList, IList> m_EntitiesToAddTable = new();
    private readonly ConcurrentDictionary<IList, IList> m_EntitiesToRemoveTable = new();
    private readonly Dictionary<Type, IList> m_ComponentTypeToEntitiesTable = new();

    public int Query<TComponent>(IEntity<TComponent>[] buffer) where TComponent : struct
    {
        var componentType = typeof(TComponent);
        if (m_ComponentTypeToEntitiesTable.TryGetValue(componentType, out var entities))
        {
            var entitiesList = (List<IEntity<TComponent>>)entities;
            var entityCount = entitiesList.Count;
            entitiesList.CopyTo(buffer);
            return entityCount;
        }
        return 0;
    }

    public void BeginFrame()
    {
        m_IsInFrame = true;
    }

    public void EndFrame()
    {
        m_IsInFrame = false;
        foreach (var (entities, cachedEntities) in m_EntitiesToAddTable)
        {
            foreach (var cachedEntity in cachedEntities)
                entities.Add(cachedEntity);

            cachedEntities.Clear();
        }

        foreach (var (entities, cachedEntities) in m_EntitiesToRemoveTable)
        {
            foreach (var cachedEntity in cachedEntities)
                entities.Remove(cachedEntity);

            cachedEntities.Clear();
        }
    }

    public void Add<TComponent>(IEntity<TComponent> entity) where TComponent : struct
    {
        var componentType = typeof(TComponent);

        //Logger.Trace($"Adding {componentType} to {entity.GetHashCode()}");

        IList entitiesToAddCache;
        if (!m_ComponentTypeToEntitiesTable.TryGetValue(componentType, out var entities))
        {
            //Logger.Trace($"Did not find list for type: {componentType}");
            entities = new List<IEntity<TComponent>>();
            entitiesToAddCache = new List<IEntity<TComponent>>();
            m_ComponentTypeToEntitiesTable[componentType] = entities;
            m_EntitiesToAddTable[entities] = entitiesToAddCache;
            m_EntitiesToRemoveTable[entities] = new List<IEntity<TComponent>>();
        }

        if (m_IsInFrame)
        {
            //Logger.Trace("Is In Frame");
            entitiesToAddCache = m_EntitiesToAddTable[entities];
            entitiesToAddCache.Add(entity);
        }
        else
        {
            //Logger.Trace("Is Out Frame");
            entities.Add(entity);
        }
    }

    public void Remove<TComponent>(IEntity<TComponent> entity) where TComponent : struct
    {
        var componentType = typeof(TComponent);
        if (m_ComponentTypeToEntitiesTable.TryGetValue(componentType, out var entities))
        {
            //Logger.Trace($"Removing {entity.GetHashCode()}");
            if (m_IsInFrame)
            {
                var entitiesToRemoveCache = m_EntitiesToRemoveTable[entities];
                entitiesToRemoveCache.Add(entity);
            }
            else
            {
                entities.Remove(entity);
            }
        }
    }
}