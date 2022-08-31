namespace SampleGames;

public readonly struct Grid
{
    public int Width { get; }
    public int Height { get; }

    public Grid(int width, int height)
    {
        Width = width;
        Height = height;
    }
}