namespace Bricks.ECS;

[Flags]
public enum RenderableKind
{
    Ball = 1 << 0,
    Brick = 1 << 1,
}