namespace Bricks.ECS;

public record struct Renderable
{
    public required RenderableKind Kind { get; set; }
}