namespace NodeGraphApp;

public readonly struct Padding
{
    public float Left { get; init; }
    public float Top { get; init; }
    public float Right { get; init; }
    public float Bottom { get; init; }

    public static Padding All(float value)
    {
        return new Padding
        {
            Left = value,
            Top = value,
            Right = value,
            Bottom = value
        };
    }
}