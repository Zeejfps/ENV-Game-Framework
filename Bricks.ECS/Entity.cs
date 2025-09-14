namespace Bricks.ECS;

[Flags]
public enum Tags
{
    None = 0,
    Ball = 1 << 0
}

public sealed record Entity
{
    private static ulong s_Id = 1;

    private readonly ulong _id;
    
    public Entity(ulong id)
    {
        _id = id;
    }

    public Tags Tags { get; set; }

    public static Entity New()
    {
        return new Entity(s_Id++);
    }
}