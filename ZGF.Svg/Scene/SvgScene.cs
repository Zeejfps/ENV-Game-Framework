namespace ZGF.Svg.Scene;

/// <summary>Flat retained scene: no element tree, no strings — just segments and paint commands.</summary>
internal sealed class SvgScene
{
    public required PathSegment[] Segments { get; init; }
    public required SvgDrawCommand[] Commands { get; init; }
    public required float[] DashValues { get; init; }

    public static readonly SvgScene Empty = new()
    {
        Segments = [],
        Commands = [],
        DashValues = [],
    };
}
