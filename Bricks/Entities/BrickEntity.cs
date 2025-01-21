using System.Numerics;
using Bricks.Archetypes;
using Bricks.Repos;

namespace Bricks.Entities;

public sealed class BrickEntity : IBrick
{
    public Vector2 Position { get; init; }
    public float Width { get; init; }
    public float Height { get; init; }

    private readonly World m_World;

    private int _health;

    public BrickEntity(World world)
    {
        m_World = world;
        _health = 2;
    }

    public void Spawn()
    {
        m_World.Bricks.Add(this);
    }

    public void Despawn()
    {
        m_World.Bricks.Remove(this);
    }

    public AABB GetAABB()
    {
        var halfWidth = Width * 0.5f;
        var halfHeight = Height * 0.5f;
        var left = Position.X - halfWidth;
        var top = Position.Y - halfHeight;
        return AABB.FromLeftTopWidthHeight(left, top, Width, Height);
    }

    public bool IsDamaged => _health < 2;

    public void TakeDamage()
    {
        _health -= 1;
        if (_health == 0)
            Despawn();
    }
}