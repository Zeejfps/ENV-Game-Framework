using System.Numerics;
using CombatBeesBenchmarkV2.Components;
using CombatBeesBenchmarkV2.EcsPrototype;

namespace CombatBeesBenchmarkV2.Systems;

public sealed class AttractionAndRepellentPointAssignmentSystem : System<AttractRepelComponent>
{
    private AttractAndRepelTeamSortingSystem AttractAndRepelTeamSortingSystem { get; }
    private Random Random { get; }

    public AttractionAndRepellentPointAssignmentSystem(IWorld world, int size, AttractAndRepelTeamSortingSystem attractAndRepelTeamSortingSystem, Random random) : base(world, size)
    {
        AttractAndRepelTeamSortingSystem = attractAndRepelTeamSortingSystem;
        Random = random;
    }

    protected override void OnUpdate(float dt, ref Span<AttractRepelComponent> components)
    {
        for (var i = 0; i < components.Length; i++)
        {
            ref var component = ref components[i];
            var teamIndex = component.TeamIndex;
            component.AttractionPoint = GetRandomAliveAllyBeePosition(teamIndex);
            component.RepellentPoint = GetRandomAliveAllyBeePosition(teamIndex);
        }
    }

    private Vector3 GetRandomAliveAllyBeePosition(int teamIndex)
    {
        var aliveAlliedBees = AttractAndRepelTeamSortingSystem.GetAliveBeesForTeam(teamIndex);
        if (aliveAlliedBees.Count == 0)
            return Vector3.Zero;

        var randIndex = Random.Next(0, aliveAlliedBees.Count);
        var ally = aliveAlliedBees[randIndex];
        return ally.Movement.Position;
    }
}