namespace Core;

public readonly struct Direction : IEquatable<Direction>
{
    public static readonly Direction North = new(0, 1);
    public static readonly Direction East = new(1, 0);
    public static readonly Direction South = new(0, -1);
    public static readonly Direction West = new(-1, 0);

    public readonly int Dx;
    public readonly int Dy;
    
    public Direction(int dx, int dy)
    {
        Dx = dx;
        Dy = dy;
    }

    public bool Equals(Direction other)
    {
        return Dx == other.Dx && Dy == other.Dy;
    }

    public override bool Equals(object? obj)
    {
        return obj is Direction other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Dx, Dy);
    }

    public static bool operator ==(Direction left, Direction right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Direction left, Direction right)
    {
        return !left.Equals(right);
    }
}