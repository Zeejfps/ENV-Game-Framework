public readonly struct Color
{
    public float R { get; init; }
    public float G { get; init; }
    public float B { get; init; }
    public float A { get; init; }

    public static Color FromRGBA(float r, float g, float b, float a)
    {
        return new Color
        {
            R = r,
            G = g,
            B = b,
            A = a
        };
    }
}