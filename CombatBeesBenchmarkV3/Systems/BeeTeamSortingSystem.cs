using CombatBeesBenchmarkV3.Archetypes;
using CombatBeesBenchmarkV3.EcsPrototype;

namespace CombatBeesBenchmarkV3.Systems;

public sealed class BeeTeamSortingSystem : System<Entity, AttractableBee>
{
    private readonly Dictionary<int, HashSet<Entity>> m_TeamToBeeTable = new();
    
    public BeeTeamSortingSystem(World<Entity> world, int size) : base(world, size)
    {
    }

    protected override void OnEntityAdded(Entity entity)
    {
        if (!m_TeamToBeeTable.TryGetValue(entity.TeamIndex, out var entities))
        {
            entities = new HashSet<Entity>();
            m_TeamToBeeTable[entity.TeamIndex] = entities;
        }

        entities.Add(entity);
        base.OnEntityAdded(entity);
    }

    protected override void OnEntityRemoved(Entity entity)
    {
        if (m_TeamToBeeTable.TryGetValue(entity.TeamIndex, out var entities))
            entities.Remove(entity);
        
        base.OnEntityRemoved(entity);
    }

    protected override void OnUpdate(float dt, ref Span<AttractableBee> archetypes)
    {
    }
}