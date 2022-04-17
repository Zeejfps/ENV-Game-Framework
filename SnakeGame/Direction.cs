namespace SnakeGame;

public readonly struct Direction
{
    public static readonly Direction North = new(0, 1);
    public static readonly Direction East = new(1, 0);
    public static readonly Direction South = new(0, 1);
    public static readonly Direction West = new(0, 1);

    public readonly int Dx;
    public readonly int Dy;
    
    public Direction(int dx, int dy)
    {
        Dx = dx;
        Dy = dy;
    }
}