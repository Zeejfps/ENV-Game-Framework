namespace ZGF.Gui;

public readonly record struct Size
{
    public required float Width { get; init; }
    public required float Height { get; init; }

    public Size() { }

    [System.Diagnostics.CodeAnalysis.SetsRequiredMembers]
    public Size(float width, float height)
    {
        Width = width;
        Height = height;
    }
}