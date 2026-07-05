using System.Numerics;

namespace ZGF.Svg.Scene;

internal enum SegKind : byte
{
    Move,
    Line,
    Cubic,
    Close,
}

/// <summary>
/// One normalized path segment. <see cref="P3"/> is always the segment endpoint;
/// <see cref="P1"/>/<see cref="P2"/> are the cubic control points and unused otherwise.
/// </summary>
internal readonly struct PathSegment
{
    public readonly SegKind Kind;
    public readonly Vector2 P1;
    public readonly Vector2 P2;
    public readonly Vector2 P3;

    private PathSegment(SegKind kind, Vector2 p1, Vector2 p2, Vector2 p3)
    {
        Kind = kind;
        P1 = p1;
        P2 = p2;
        P3 = p3;
    }

    public static PathSegment MoveTo(Vector2 p) => new(SegKind.Move, default, default, p);
    public static PathSegment LineTo(Vector2 p) => new(SegKind.Line, default, default, p);
    public static PathSegment CubicTo(Vector2 c1, Vector2 c2, Vector2 p) => new(SegKind.Cubic, c1, c2, p);
    public static PathSegment ClosePath() => new(SegKind.Close, default, default, default);
}
