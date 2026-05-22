namespace ZGF.Geometry;

public readonly record struct RectI(int X, int Y, int Width, int Height)
{
    public int Right => X + Width;
    public int Bottom => Y + Height;

    public bool Contains(PointI p) =>
        p.X >= X && p.X < X + Width && p.Y >= Y && p.Y < Y + Height;
}
