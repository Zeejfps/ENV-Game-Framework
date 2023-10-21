namespace CombatBeesBenchmarkV3.EcsPrototype;

public sealed class World<TEntity>
{
    private readonly Dictionary<Type, List<ISystem>> m_TypeToEntitiesTable = new();

    public void AddSystem<TArchetype>(ISystem<TEntity, TArchetype> system)
    {
        var archetypeType = typeof(TArchetype);
        if (!m_TypeToEntitiesTable.TryGetValue(archetypeType, out var systems))
        {
            systems = new List<ISystem>();
            m_TypeToEntitiesTable[archetypeType] = systems;
        }
        systems.Add(system);
    }
    
    public void AddEntity<TArchetype>(TEntity entity)
    {
        var archetypeType = typeof(TArchetype);
        if (m_TypeToEntitiesTable.TryGetValue(archetypeType, out var systems))
        {
            foreach (var system in systems)
                ((ISystem<TEntity, TArchetype>)system).Add(entity);
        }
    }

    public void RemoveEntity<TArchetype>(TEntity entity)
    {
        var type = typeof(TArchetype);
        if (m_TypeToEntitiesTable.TryGetValue(type, out var systems))
        {
            foreach (var system in systems)
                ((ISystem<TEntity, TArchetype>)system).Remove(entity);
        }
    }
}