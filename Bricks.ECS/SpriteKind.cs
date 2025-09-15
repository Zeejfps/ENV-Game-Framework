namespace Bricks.ECS;

[Flags]
public enum SpriteKind
{
    Ball = 1 << 0,
    Brick = 1 << 1,
}