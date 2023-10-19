using System.Collections;
using System.Collections.Concurrent;
using CombatBeesBenchmarkV2.EcsPrototype;
using EasyGameFramework.Api;

namespace CombatBeesBenchmark;

public interface IEntityRepo : IEnumerable<IEntity>
{
    int Count { get; }
    void Add(IEntity entity);
    void Remove(IEntity entity);
    void Clear();
}

sealed class HashSetEntityRepo<TComponent> : IEntityRepo where TComponent : struct
{
    private readonly HashSet<IEntity<TComponent>> m_Entities = new();

    public int Count => m_Entities.Count;

    public void Add(IEntity entity)
    {
        lock (m_Entities)
        {
            m_Entities.Add((IEntity<TComponent>)entity);
        }
    }

    public void Remove(IEntity entity)
    {
        lock (m_Entities)
        {
            m_Entities.Remove((IEntity<TComponent>)entity);
        }
    }

    public void Clear()
    {
        m_Entities.Clear();
    }

    public void CopyTo(IEntity<TComponent>[] buffer)
    {
        m_Entities.CopyTo(buffer);
    }

    public IEnumerator<IEntity> GetEnumerator()
    {
        return m_Entities.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

public sealed class World : IWorld
{
    public World(ILogger logger)
    {
        Logger = logger;
    }
    
    private ILogger Logger { get; }

    public void Update(float dt)
    {
        
    }

    private bool m_IsInFrame;
    private readonly ConcurrentDictionary<IEntityRepo, IEntityRepo> m_EntitiesToAddTable = new();
    private readonly ConcurrentDictionary<IEntityRepo, IEntityRepo> m_EntitiesToRemoveTable = new();
    private readonly Dictionary<Type, IEntityRepo> m_ComponentTypeToEntitiesTable = new();

    public int Query<TComponent>(IEntity<TComponent>[] buffer) where TComponent : struct
    {
        var componentType = typeof(TComponent);
        if (m_ComponentTypeToEntitiesTable.TryGetValue(componentType, out var entities))
        {
            var entitiesList = (HashSetEntityRepo<TComponent>)entities;
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

    public void Add<TComponent>(IEntity<TComponent>? entity) where TComponent : struct
    {
        var componentType = typeof(TComponent);

        //Logger.Trace($"Adding {componentType} to {entity.GetHashCode()}");

        IEntityRepo entitiesToAddCache;
        if (!m_ComponentTypeToEntitiesTable.TryGetValue(componentType, out var entities))
        {
            //Logger.Trace($"Did not find list for type: {componentType}");
            entities = new HashSetEntityRepo<TComponent>();
            entitiesToAddCache = new HashSetEntityRepo<TComponent>();
            m_ComponentTypeToEntitiesTable[componentType] = entities;
            m_EntitiesToAddTable[entities] = entitiesToAddCache;
            m_EntitiesToRemoveTable[entities] = new HashSetEntityRepo<TComponent>();
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

    public void Remove<TComponent>(IEntity<TComponent>? entity) where TComponent : struct
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