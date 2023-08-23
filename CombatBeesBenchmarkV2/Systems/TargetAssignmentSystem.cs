using CombatBeesBenchmarkV2.Components;
using CombatBeesBenchmarkV2.EcsPrototype;

namespace CombatBeesBenchmarkV2.Systems;

public sealed class TargetAssignmentSystem : System<NeedTargetComponent>
{
    private TargetTeamSortingSystem TargetTeamSortingSystem { get; }
    private Random Random { get; }

    public TargetAssignmentSystem(IWorld world, int size, TargetTeamSortingSystem targetTeamSortingSystem, Random random) : base(world, size)
    {
        TargetTeamSortingSystem = targetTeamSortingSystem;
        Random = random;
    }

    protected override void OnUpdate(float dt, ref Span<NeedTargetComponent> components)
    {
        var teams = TargetTeamSortingSystem.GetTeams();

        for (var i = 0; i < components.Length; i++)
        {
            ref var component = ref components[i];
            var enemies = teams[1 - component.TeamIndex];
            if (enemies.Count == 0)
                continue;

            component.Target = enemies[Random.Next(0, enemies.Count)];
        }
    }
}