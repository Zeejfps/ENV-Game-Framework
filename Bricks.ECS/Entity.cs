namespace Bricks.ECS;

public record struct Entity
{
    private static ulong s_Id = 1;

    private readonly ulong _id;
    
    public Entity(ulong id)
    {
        _id = id;
    }
    
    public static Entity New()
    {
        return new Entity(s_Id++);
    }
}