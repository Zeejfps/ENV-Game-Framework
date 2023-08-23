using CombatBeesBenchmarkV2.Components;
using CombatBeesBenchmarkV2.EcsPrototype;

namespace CombatBeesBenchmarkV2.Systems;

public sealed class TargetTeamSortingSystem : System<TargetComponent>
{
    private readonly List<IEntity<TargetComponent>>[] m_Teams = new List<IEntity<TargetComponent>>[2];

    public TargetTeamSortingSystem(IWorld world, int size) : base(world, size)
    {
        m_Teams[0] = new List<IEntity<TargetComponent>>();
        m_Teams[1] = new List<IEntity<TargetComponent>>();
    }

    protected override void OnUpdate(float dt, ref Span<TargetComponent> components)
    {
        m_Teams[0].Clear();
        m_Teams[1].Clear();
        for (var i = 0; i < components.Length; i++)
        {
            ref var component = ref components[i];
            var entity = m_Entities[i];
            m_Teams[component.TeamIndex].Add(entity);
        }
    }

    public List<IEntity<TargetComponent>>[] GetTeams()
    {
        return m_Teams;
    }
}