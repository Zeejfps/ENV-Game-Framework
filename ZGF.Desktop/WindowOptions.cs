namespace ZGF.Desktop;

public readonly struct WindowOptions
{
    public required int WidthPoints { get; init; }
    public required int HeightPoints { get; init; }
    public required string Title { get; init; }
    public bool IsUndecorated { get; init; }
}
