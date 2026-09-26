using ZGF.Geometry;

namespace ZGF.Gui;

/// <summary>
/// The device-pixel grid a window draws on, expressed in logical points: one point is
/// <see cref="Scale"/> device pixels. Layout and drawing both round to it, so every edge lands on a
/// whole pixel and two edges that meet in logical space meet on screen.
/// </summary>
public readonly record struct PixelGrid
{
    // Whole points land on exact half pixels at 125% and 150%, where float noise from layout would
    // round one shared edge two ways. Moving the tie point just below .5 keeps it off that value.
    private const float SnapBias = 0.5f + 1f / 32f;

    // Measured content only rounds up past float noise, so text that fits exactly isn't handed an
    // extra pixel it doesn't need.
    private const float CeilTolerance = 1f / 1024f;

    public float Scale { get; }

    public PixelGrid(float scale)
    {
        if (!(scale > 0f) || !float.IsFinite(scale))
            throw new ArgumentOutOfRangeException(nameof(scale), scale, "A pixel grid needs a positive, finite scale.");
        Scale = scale;
    }

    /// <summary>The grid line nearest to a coordinate.</summary>
    public float Snap(float logical) => MathF.Floor(logical * Scale + SnapBias) / Scale;

    /// <summary>A rect whose four edges are each snapped; its size follows from the snapped edges.</summary>
    public RectF Snap(RectF rect)
    {
        var left = Snap(rect.Left);
        var bottom = Snap(rect.Bottom);
        return new RectF(left, bottom, MathF.Max(0f, Snap(rect.Right) - left), MathF.Max(0f, Snap(rect.Top) - bottom));
    }

    /// <summary>The whole-pixel length nearest to a size, for sizes someone chose.</summary>
    public float RoundLength(float logical) =>
        MathF.Round(logical * Scale, MidpointRounding.AwayFromZero) / Scale;

    /// <summary>The smallest whole-pixel length that holds a size, for sizes content needs.</summary>
    public float CeilLength(float logical) => MathF.Ceiling(logical * Scale - CeilTolerance) / Scale;
}
