namespace SnakeGame;

public readonly struct Direction
{
    public static readonly Direction North = new Direction(0, 1);

    public readonly int Dx;
    public readonly int Dy;
    
    public Direction(int dx, int dy)
    {
        Dx = dx;
        Dy = dy;
    }
}