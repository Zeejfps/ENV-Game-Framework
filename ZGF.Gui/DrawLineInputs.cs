using ZGF.Geometry;

namespace ZGF.Gui;

public enum LineCap
{
    Round = 0,
    Butt = 1,
    Square = 2,
}

public readonly struct DrawLineInputs
{
    public required PointF Start { get; init; }
    public required PointF End { get; init; }
    public required float Thickness { get; init; }
    public required uint Color { get; init; }
    public required int ZIndex { get; init; }

    // Optional stroke styling. Defaults reproduce a solid, round-capped line.
    public LineCap Cap { get; init; }
    public float DashLength { get; init; } // with GapLength > 0 enables dashing
    public float GapLength { get; init; }
    public uint? GradientEndColor { get; init; } // null = solid Color; else Color -> this along the line
}
