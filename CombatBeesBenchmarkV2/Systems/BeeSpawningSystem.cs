using System.Numerics;
using CombatBeesBenchmark;
using CombatBeesBenchmarkV2.Archetype;
using CombatBeesBenchmarkV2.EcsPrototype;

namespace CombatBeesBenchmarkV2.Systems;

public sealed class BeeSpawningSystem : System<SpawnableBeeArchetype>
{
    private readonly Random m_Random;
    
    public BeeSpawningSystem(IWorld world, int size, Random random) : base(world, size)
    {
        m_Random = random;
    }

    protected override void OnUpdate(float dt, ref Span<SpawnableBeeArchetype> archetypes)
    {
        for (var i = 0; i < archetypes.Length; i++)
        {
            ref var archetype = ref archetypes[i];
            var spawnPosition = Vector3.UnitX * (-100f * .4f + 100f * .8f * archetype.In.TeamIndex);
            archetype.Out.SpawnPosition = spawnPosition;
            archetype.Out.Size = m_Random.NextSingleInRange(0.25f, 0.5f);
        }
    }
}