using System.Numerics;

public sealed class Link
{
    public Vector2 StartPosition { get; set; }
    public Vector2 EndPosition { get; set; }
    public bool IsHovered { get; set; }
    public bool IsSelected { get; set; }
}