using System.Numerics;
using CombatBeesBenchmark;
using CombatBeesBenchmarkV3.Archetypes;
using CombatBeesBenchmarkV3.EcsPrototype;

namespace CombatBeesBenchmarkV3.Systems;

public sealed class BeeSpawningSystem : System<Entity, SpawnableBee>
{
    private readonly Random m_Random;

    public BeeSpawningSystem(World<Entity> world, int size, Random random) : base(world, size)
    {
        m_Random = random;
    }

    protected override void OnUpdate(float dt, ref Span<SpawnableBee> archetypes)
    {
        for (var i = 0; i < archetypes.Length; i++)
        {
            ref var archetype = ref archetypes[i];
            var spawnPosition = Vector3.UnitX * (-100f * .4f + 100f * .8f * archetype.In.TeamIndex);
            archetype.Out.SpawnPosition = spawnPosition;
        }

        for (var i = 0; i < archetypes.Length; i++)
        {
            ref var archetype = ref archetypes[i];
            archetype.Out.Size = m_Random.NextSingleInRange(0.25f, 0.5f);
        }
    }

    protected override void OnWrite()
    {
        foreach (var entity in Entities)
        {
            World.RemoveEntity<SpawnableBee>(entity);
            World.AddEntity<AliveBee>(entity);
        }
    }
}