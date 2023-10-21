namespace CombatBeesBenchmarkV3.EcsPrototype;

public abstract class System<TEntity, TArchetype> : ISystem<TEntity, TArchetype> where TEntity : IEntity<TArchetype>
{
    protected World<TEntity> World { get; }
    protected IEnumerable<TEntity> Entities => m_Entities;

    private readonly TArchetype[] m_Archetypes;
    private readonly List<TEntity> m_Entities = new();

    protected System(World<TEntity> world, int size)
    {
        World = world;
        m_Archetypes = new TArchetype[size];
    }

    public void Add(TEntity entity)
    {
        m_Entities.Add(entity);
    }

    public void Remove(TEntity entity)
    {
        m_Entities.Remove(entity);
    }

    public void Tick(float dt)
    {
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
            entity.Write(ref component);
        });
        OnRead();
    }

    private void Update(float dt)
    {
        var count = m_Entities.Count;
        var components = m_Archetypes.AsSpan(0, count);
        OnUpdate(dt, ref components);
    }

    private void Write()
    {
        var entityCount = m_Entities.Count;
        Parallel.For(0, entityCount, (i) =>
        {
            var entity = m_Entities[i];
            ref var component = ref m_Archetypes[i];
            entity.Read(ref component);
        });
        OnWrite();
    }

    protected abstract void OnRead();
    protected abstract void OnUpdate(float dt, ref Span<TArchetype> archetypes);
    protected abstract void OnWrite();
}