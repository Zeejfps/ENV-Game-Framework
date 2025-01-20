using System.Numerics;

namespace Bricks.Entities;

public sealed class BrickEntity : IBrick
{
    public Vector2 Position { get; init; }
    public float Width { get; init; }
    public float Height { get; init; }

    private readonly BricksRepo _bricksRepo;

    private int _health;

    public BrickEntity(BricksRepo bricksRepo)
    {
        _bricksRepo = bricksRepo;
        _health = 2;
    }

    public void Spawn()
    {
        _bricksRepo.Add(this);
    }

    public void Despawn()
    {
        _bricksRepo.Remove(this);
    }

    public Rectangle CalculateBoundsRectangle()
    {
        var halfWidth = Width * 0.5f;
        var halfHeight = Height * 0.5f;
        var left = Position.X - halfWidth;
        var top = Position.Y - halfHeight;
        return Rectangle.LeftTopWidthHeight(left, top, Width, Height);
    }

    public bool IsDamaged => _health < 2;

    public void TakeDamage()
    {
        _health -= 1;
        if (_health == 0)
            Despawn();
    }
}