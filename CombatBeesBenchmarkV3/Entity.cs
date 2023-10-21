using System.Numerics;
using CombatBeesBenchmarkV3.Archetypes;
using CombatBeesBenchmarkV3.EcsPrototype;

namespace CombatBeesBenchmarkV3;

public sealed class Entity : IEntity<SpawnableBee>
{
    public int TeamIndex { get; set; }
    
    private Vector3 m_Position;
    private float m_Size;
    
    public void Write(ref SpawnableBee archetype)
    {
        archetype.In.TeamIndex = TeamIndex;
    }

    public void Read(ref SpawnableBee archetype)
    {
        m_Position = archetype.Out.SpawnPosition;
        m_Size = archetype.Out.Size;
    }
}