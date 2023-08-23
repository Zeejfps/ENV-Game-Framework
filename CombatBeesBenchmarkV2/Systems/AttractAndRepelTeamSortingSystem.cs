using CombatBeesBenchmarkV2.Components;
using CombatBeesBenchmarkV2.EcsPrototype;

namespace CombatBeesBenchmarkV2.Systems;

public sealed class AttractAndRepelTeamSortingSystem : ReadOnlySystem<AliveBeeComponent>
{
    private readonly List<AliveBeeComponent>[] m_AliveBees = new List<AliveBeeComponent>[2];

    public AttractAndRepelTeamSortingSystem(IWorld world, int size) : base(world, size)
    {
        m_AliveBees[0] = new List<AliveBeeComponent>();
        m_AliveBees[1] = new List<AliveBeeComponent>();
    }

    protected override void OnUpdate(float dt, ref Span<AliveBeeComponent> components)
    {
        m_AliveBees[0].Clear();
        m_AliveBees[1].Clear();
        for (var i = 0; i < components.Length; i++)
        {
            ref var component = ref components[i];
            m_AliveBees[component.TeamIndex].Add(component);
        }
    }

    public IList<AliveBeeComponent> GetAliveBeesForTeam(int beeTeamIndex)
    {
        return m_AliveBees[beeTeamIndex];
    }
}