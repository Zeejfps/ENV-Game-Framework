using CombatBeesBenchmarkV2.Components;
using CombatBeesBenchmarkV2.EcsPrototype;

namespace CombatBeesBenchmarkV2.Systems;

public sealed class AttractAndRepelTeamSortingSystem : ReadOnlySystem<CanAttractOrRepelComponent>
{
    private readonly List<CanAttractOrRepelComponent>[] m_AliveBees = new List<CanAttractOrRepelComponent>[2];

    public AttractAndRepelTeamSortingSystem(IWorld world, int size) : base(world, size)
    {
        m_AliveBees[0] = new List<CanAttractOrRepelComponent>();
        m_AliveBees[1] = new List<CanAttractOrRepelComponent>();
    }

    protected override void OnUpdate(float dt, ref Span<CanAttractOrRepelComponent> components)
    {
        m_AliveBees[0].Clear();
        m_AliveBees[1].Clear();
        for (var i = 0; i < components.Length; i++)
        {
            ref var component = ref components[i];
            m_AliveBees[component.TeamIndex].Add(component);
        }
    }

    public IList<CanAttractOrRepelComponent> GetAliveBeesForTeam(int beeTeamIndex)
    {
        return m_AliveBees[beeTeamIndex];
    }
}