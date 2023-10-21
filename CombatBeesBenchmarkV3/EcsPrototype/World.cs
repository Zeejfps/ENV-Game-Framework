namespace CombatBeesBenchmarkV3.EcsPrototype;

public sealed class World<TEntity>
{
    private readonly Dictionary<Type, ISystem> m_TypeToEntitiesTable = new();

    public void RegisterSystem<TArchetype>(ISystem<TEntity, TArchetype> system)
    {
        var archetypeType = typeof(TArchetype);
        m_TypeToEntitiesTable[archetypeType] = system;
    }
    
    public void UnregisterSystem<TArchetype>(TEntity entity)
    {
        var archetypeType = typeof(TArchetype);
        m_TypeToEntitiesTable.Remove(archetypeType);
    }

    public void RemoveEntity<TArchetype>(TEntity entity)
    {
        var type = typeof(TArchetype);
        if (m_TypeToEntitiesTable.TryGetValue(type, out var system))
            ((ISystem<TEntity, TArchetype>)system).Remove(entity);
    }

    public void Tick(float dt)
    {
        foreach (var system in m_TypeToEntitiesTable.Values)
            system.Tick(dt);
    }
}