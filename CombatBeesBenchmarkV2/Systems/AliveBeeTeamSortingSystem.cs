using CombatBeesBenchmarkV2.Components;
using CombatBeesBenchmarkV2.EcsPrototype;

namespace CombatBeesBenchmarkV2.Systems;

public sealed class AliveBeeTeamSortingSystem : ReadOnlySystem<AliveBeeComponent>
{
    private readonly List<IEntity<AliveBeeComponent>>[] m_AliveBees = new List<IEntity<AliveBeeComponent>>[2];

    public AliveBeeTeamSortingSystem(IWorld world, int size) : base(world, size)
    {
        m_AliveBees[0] = new List<IEntity<AliveBeeComponent>>();
        m_AliveBees[1] = new List<IEntity<AliveBeeComponent>>();
    }

    protected override void OnUpdate(float dt, ref Span<AliveBeeComponent> components)
    {
        m_AliveBees[0].Clear();
        m_AliveBees[1].Clear();
        for (var i = 0; i < components.Length; i++)
        {
            ref var component = ref components[i];
            var entity = GetEntity(i);
            m_AliveBees[component.TeamIndex].Add(entity);
        }
    }
}