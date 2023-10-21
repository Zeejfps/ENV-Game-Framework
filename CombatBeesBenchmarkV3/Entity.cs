using System.Numerics;
using CombatBeesBenchmarkV3.Archetypes;
using CombatBeesBenchmarkV3.EcsPrototype;

namespace CombatBeesBenchmarkV3;

public sealed class Entity : 
    IEntity<SpawnableBee>,
    IEntity<RenderableBee>,
    IEntity<AliveMovableBee>
{
    public int TeamIndex { get; set; }
    public Vector4 Color { get; set; }
        
    private Vector3 m_Position;
    private Vector3 m_Velocity;
    private Vector3 m_MoveDirection;
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

    public void Write(ref RenderableBee archetype)
    {
        archetype.Color = Color;

        var size = m_Size;
        archetype.ModelMatrix = Matrix4x4.CreateScale(size, size, size)
                                //* Matrix4x4.CreateLookAt(Vector3.Zero, LookDirection, Vector3.UnitY)
                                * Matrix4x4.CreateTranslation(m_Position);
        
        // Console.WriteLine($"Position: {m_Position}");
        // Console.WriteLine($"Size: {m_Size}");
        // Console.WriteLine(archetype.ModelMatrix);
    }

    public void Read(ref RenderableBee archetype)
    {
    }

    public void Write(ref AliveMovableBee archetype)
    {
        archetype.Movement.Position = m_Position;
        archetype.Movement.Velocity = m_Velocity;
        archetype.MoveDirection = m_MoveDirection;
        archetype.LookDirection = Vector3.UnitX;
        archetype.RepellentPoint = Vector3.Zero;
        archetype.AttractionPoint = Vector3.One;
    }

    public void Read(ref AliveMovableBee archetype)
    {
        m_Position = archetype.Movement.Position;
        m_Velocity = archetype.Movement.Velocity;
        m_MoveDirection = archetype.MoveDirection;
    }
}