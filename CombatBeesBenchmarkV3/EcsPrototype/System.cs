namespace CombatBeesBenchmarkV3.EcsPrototype;

public abstract class System<TEntity, TArchetype> : ISystem<TEntity, TArchetype> where TEntity : IEntity<TArchetype>
{
    protected World<TEntity> World { get; }
    protected IEnumerable<TEntity> Entities => m_Entities;

    private readonly TArchetype[] m_Archetypes;
    private readonly List<TEntity> m_Entities = new();
    private readonly HashSet<TEntity> m_EntitiesToAddBuffer = new();
    private readonly HashSet<TEntity> m_EntitiesToRemoveBuffer = new();

    protected System(World<TEntity> world, int size)
    {
        World = world;
        m_Archetypes = new TArchetype[size];
    }

    public void Add(TEntity entity)
    {
        m_EntitiesToRemoveBuffer.Remove(entity);
        m_EntitiesToAddBuffer.Add(entity);
    }

    public void Remove(TEntity entity)
    {
        m_EntitiesToAddBuffer.Remove(entity);
        m_EntitiesToRemoveBuffer.Add(entity);
    }

    public void Tick(float dt)
    {
        foreach (var entity in m_EntitiesToAddBuffer)
        {
            m_Entities.Add(entity);
            OnEntityAdded(entity);
        }
        m_EntitiesToAddBuffer.Clear();

        foreach (var entity in m_EntitiesToRemoveBuffer)
        {
            m_Entities.Remove(entity);
            OnEntityRemoved(entity);
        }
        m_EntitiesToRemoveBuffer.Clear();
        
        Read();
        Update(dt);
        Write();
    }

    private void Read()
    {
        var entityCount = m_Entities.Count;
        Parallel.For(0, entityCount, (i) =>
        {
            var entity = m_Entities[i];
            ref var component = ref m_Archetypes[i];
            entity.WriteTo(ref component);
        });
        OnRead();
    }

    private void Update(float dt)
    {
        var entityCount = m_Entities.Count;
        var components = m_Archetypes.AsMemory(0, entityCount);
        OnUpdate(dt, ref components);
    }

    private void Write()
    {
        var entityCount = m_Entities.Count;
        for (var i = 0; i < entityCount; i++)
        {
            var entity = m_Entities[i];
            ref var component = ref m_Archetypes[i];
            entity.ReadFrom(ref component);
        }
        OnWrite();
    }

    protected virtual void OnEntityAdded(TEntity entity){}
    protected virtual void OnEntityRemoved(TEntity entity){}
    protected virtual void OnRead(){}
    protected abstract void OnUpdate(float dt, ref Memory<TArchetype> memory);
    protected virtual void OnWrite(){}
}