using CombatBeesBenchmarkV2.Components;
using CombatBeesBenchmarkV2.EcsPrototype;

namespace CombatBeesBenchmarkV2.Systems;

public sealed class UpdateTargetSystem : System<HasTargetComponent>
{
    private TargetTeamSortingSystem TargetTeamSortingSystem { get; }
    private Random Random { get; }

    private readonly Dictionary<IEntity<HasTargetComponent>, IEntity<TargetComponent>> m_EntityToTargetTable = new();

    public UpdateTargetSystem(IWorld world, int size, TargetTeamSortingSystem targetTeamSortingSystem, Random random) : base(world, size)
    {
        TargetTeamSortingSystem = targetTeamSortingSystem;
        Random = random;
    }

    protected override void OnUpdate(float dt, ref Span<HasTargetComponent> components)
    {
        for (var i = 0; i < components.Length; i++)
        {
            var entity = m_Entities[i];
            ref var component = ref components[i];

            var targetComponent = new TargetComponent();
            if (!m_EntityToTargetTable.TryGetValue(entity, out var target))
            {
                target = FindNewTarget(component.TeamIndex);
                m_EntityToTargetTable[entity] = target;
            }
            else
            {
                target.Into(ref targetComponent);
                if (targetComponent.IsDead)
                {
                    target = FindNewTarget(component.TeamIndex);
                    target.Into(ref targetComponent);
                    m_EntityToTargetTable[entity] = target;
                }
            }

            component.TargetPosition = targetComponent.Position;
        }
    }

    private IEntity<TargetComponent> FindNewTarget(int teamIndex)
    {
        var teams = TargetTeamSortingSystem.GetTeams();
        var enemyTeam = teams[1 - teamIndex];
        var randIndex = Random.Next(0, enemyTeam.Count);
        var enemy = enemyTeam[randIndex];
        return enemy;
    }
}