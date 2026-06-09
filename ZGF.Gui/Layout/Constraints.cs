using System.Diagnostics;

namespace ZGF.Gui;

public enum Axis
{
    Horizontal,
    Vertical
}

/// <summary>
/// Immutable box constraints (min/max on each axis) flowing top-down through a layout pass.
/// A view returns a desired <see cref="Size"/> from <c>MeasureContent</c>; the base measure
/// clamps it into this box via <see cref="Constrain"/>. Replaces the V1
/// Width/WidthConstraint/Min*/Max* mutable-field soup. See docs/W1-Layout.md.
/// </summary>
public readonly record struct Constraints(
    float MinWidth, float MaxWidth,
    float MinHeight, float MaxHeight)
{
    public static Constraints Tight(Size s)
    {
        Debug.Assert(float.IsFinite(s.Width) && float.IsFinite(s.Height),
            "Tight constraint cannot be infinite — a parent tried to force an unbounded size " +
            "(usually Stretch cross-axis or grow under an unbounded main). See docs/W1-Layout.md.");
        return new Constraints(s.Width, s.Width, s.Height, s.Height);
    }

    public static Constraints Tight(float width, float height) =>
        Tight(new Size(width, height));

    public static Constraints Loose(float maxW, float maxH) =>
        new(0, maxW, 0, maxH);

    public static readonly Constraints Unbounded =
        new(0, float.PositiveInfinity, 0, float.PositiveInfinity);

    public Constraints LooseMainUnbounded(Axis axis) => axis == Axis.Vertical
        ? this with { MinHeight = 0, MaxHeight = float.PositiveInfinity }
        : this with { MinWidth = 0, MaxWidth = float.PositiveInfinity };

    /// <summary>Clamp a desired size into the allowed box. The single point where sizing rules live.</summary>
    public Size Constrain(Size s) => new(
        Math.Clamp(s.Width, MinWidth, MaxWidth),
        Math.Clamp(s.Height, MinHeight, MaxHeight));

    /// <summary>Shrink the box by chrome (padding/border) on each axis: maxes reduced (∞ stays ∞), mins floored at 0.</summary>
    public Constraints Deflate(float horizontal, float vertical) => new(
        Math.Max(0, MinWidth - horizontal),
        HasBoundedWidth ? Math.Max(0, MaxWidth - horizontal) : MaxWidth,
        Math.Max(0, MinHeight - vertical),
        HasBoundedHeight ? Math.Max(0, MaxHeight - vertical) : MaxHeight);

    public bool HasBoundedWidth => !float.IsPositiveInfinity(MaxWidth);
    public bool HasBoundedHeight => !float.IsPositiveInfinity(MaxHeight);

    public bool HasBoundedMain(Axis a) => a == Axis.Vertical ? HasBoundedHeight : HasBoundedWidth;
    public bool HasBoundedCross(Axis a) => a == Axis.Vertical ? HasBoundedWidth : HasBoundedHeight;
}

internal static class AxisExtensions
{
    public static float Main(this Axis a, Size s) => a == Axis.Vertical ? s.Height : s.Width;
    public static float Cross(this Axis a, Size s) => a == Axis.Vertical ? s.Width : s.Height;
    public static Size Pack(this Axis a, float main, float cross) =>
        a == Axis.Vertical ? new Size(cross, main) : new Size(main, cross);
}
