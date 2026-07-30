using ZGF.Geometry;

namespace ZGF.Gui.Views;

public enum Axis
{
    Horizontal,
    Vertical
}

public enum MainAxisAlignment
{
    Start,    // Pack items to the start (left)
    Center,       // Pack items in the center
    End,      // Pack items to the end (right)
    SpaceBetween, // Evenly distribute items, first at start, last at end
    SpaceAround,  // Evenly distribute items with half-size spaces at the ends
    SpaceEvenly   // Evenly distribute items with equal space all around
}

public enum CrossAxisAlignment
{
    Start, // Align to the top
    Center,    // Align to the vertical center
    End,   // Align to the bottom
    Stretch    // Stretch to fill the container's cross size
}

/// <summary>
/// A flex stack along one <see cref="Axis"/>. Replaces the four V1 containers
/// (FlexColumnView / FlexRowView / ColumnView / RowView) with one algorithm: the layout body
/// is written once; only the per-child measure and placement primitives swap main/cross.
/// Children opt into growth by wrapping in a <see cref="FlexItem"/>.
/// <para>
/// Measurement and layout size children through the same primitives, so every child's height is
/// measured at exactly the width it is laid out at — the invariant wrapping content depends on to
/// report a height it can actually draw within.
/// </para>
/// </summary>
public class FlexView : View
{
    public new ChildrenCollection Children => base.Children;

    public Axis Axis { get; init; } = Axis.Vertical;

    public float Gap
    {
        get;
        set => SetField(ref field, value);
    }

    public CrossAxisAlignment CrossAxisAlignment
    {
        get;
        set => SetField(ref field, value);
    }

    public MainAxisAlignment MainAxisAlignment
    {
        get;
        set => SetField(ref field, value);
    }

    private bool Vert => Axis == Axis.Vertical;

    protected override float MeasureWidthIntrinsic()
    {
        if (Width.IsSet) return Width;
        if (Vert) return MeasureChildrenWidth();
        return SumBases(crossExtent: 0f);
    }

    protected override float MeasureHeightIntrinsic(float availableWidth)
    {
        if (Height.IsSet) return Height;
        return Vert ? SumBases(availableWidth) : RowHeight(availableWidth);
    }

    // Sum of the visible children's main-axis bases plus gaps. The intrinsic main size: grow and
    // shrink only apply once a parent imposes an extent.
    private float SumBases(float crossExtent)
    {
        var total = 0f;
        var count = 0;
        foreach (var child in Children)
        {
            if (!child.IsVisible) continue;
            total += MainBasis(child, crossExtent);
            count++;
        }
        return total + (count > 0 ? (count - 1) * Gap : 0f);
    }

    // The tallest child, each measured at the width the same distribution will grant it in layout.
    // Measuring at the row's full width instead would let a child report the height of one wrap and
    // then draw at another, overflowing whatever sits below the row.
    private float RowHeight(float availableWidth)
    {
        // Non-positive means "unconstrained": there is no extent to distribute, so each child falls
        // back to its own intrinsic width.
        var unconstrained = availableWidth <= 0f;
        var slack = unconstrained ? default : Distribute(availableWidth, crossExtent: 0f);
        var height = 0f;
        foreach (var child in Children)
        {
            if (!child.IsVisible) continue;
            var width = unconstrained ? availableWidth : RowChildWidth(child, slack, out _);
            var childHeight = child.MeasureHeight(width);
            if (childHeight > height) height = childHeight;
        }
        return height;
    }

    protected override void OnLayoutChildren()
    {
        var pos = Position;
        if (Children.Count == 0) return;
        var rtl = IsRtl;

        var mainExtent = Vert ? pos.Height : pos.Width;
        var crossExtent = Vert ? pos.Width : pos.Height;

        var slack = Distribute(mainExtent, crossExtent);
        if (slack.VisibleCount == 0) return;

        var remaining = slack.Remaining;
        var visibleCount = slack.VisibleCount;
        var mainOffset = 0f;
        var interItem = 0f;
        if (remaining > 0)
        {
            switch (MainAxisAlignment)
            {
                case MainAxisAlignment.End: mainOffset = remaining; break;
                case MainAxisAlignment.Center: mainOffset = remaining / 2f; break;
                case MainAxisAlignment.SpaceBetween: interItem = visibleCount > 1 ? remaining / (visibleCount - 1) : 0; break;
                case MainAxisAlignment.SpaceAround: interItem = remaining / visibleCount; mainOffset = interItem / 2f; break;
                case MainAxisAlignment.SpaceEvenly: interItem = remaining / (visibleCount + 1); mainOffset = interItem; break;
            }
        }

        // Y-up: a column advances down from the top; a row advances right from the left.
        var cursor = Vert ? pos.Top - mainOffset : pos.Left + mainOffset;
        List<View>? deferredGrow = null;

        foreach (var child in Children)
        {
            if (!child.IsVisible) continue;
            var grow = GrowOf(child);

            // A column resolves its cross size (width) first because its main size is
            // height-for-width; a row resolves main (width) first for exactly the same reason.
            float finalMain, finalCross;
            bool shrunk;
            if (Vert)
            {
                finalCross = ColumnChildWidth(child, crossExtent);
                finalMain = ColumnChildHeight(child, finalCross, slack, out shrunk);
            }
            else
            {
                finalMain = RowChildWidth(child, slack, out shrunk);
                finalCross = RowChildHeight(child, crossExtent, finalMain);
            }

            var crossPos = CrossAxisAlignment == CrossAxisAlignment.Stretch
                ? Vert ? pos.Left : pos.Bottom
                : CrossPosition(pos, finalCross, crossExtent);

            if (Vert)
            {
                child.LeftConstraint = crossPos;
                child.BottomConstraint = cursor - finalMain;
                child.WidthConstraint = finalCross;
                child.HeightConstraint = finalMain;
            }
            else
            {
                child.LeftConstraint = cursor;
                child.BottomConstraint = crossPos;
                child.WidthConstraint = finalMain;
                child.HeightConstraint = finalCross;
            }

            // Mirror the (LTR-computed) horizontal extent within the container: the same transform
            // flips a Row's main axis and a Column's cross axis, reversing visual order and swapping
            // Start/End/SpaceBetween without special-casing each. Vertical coords are left alone.
            if (rtl)
                child.LeftConstraint = pos.Left + pos.Right - child.LeftConstraint - child.WidthConstraint;

            if (grow > 0 || shrunk)
                (deferredGrow ??= new List<View>()).Add(child);
            else
                child.LayoutSelf();

            cursor += Vert ? -(finalMain + Gap + interItem) : finalMain + Gap + interItem;
        }

        if (deferredGrow != null)
            foreach (var child in deferredGrow)
                child.LayoutSelf();
    }

    private static float GrowOf(View child) => child is FlexItem item ? (float)item.Grow : 0f;

    private static float ShrinkOf(View child) => child is FlexItem item ? (float)item.Shrink : 0f;

    /// <summary>The main-axis surplus (negative when the children overflow) left after every visible
    /// child's basis and the gaps, together with the weights it is handed out by.</summary>
    private readonly record struct MainSlack(
        float Remaining, float TotalGrow, float TotalShrink, int VisibleCount);

    // The one place the main axis is divided up. Layout and height-for-width measurement both size
    // children through this and MainSize, so a child can never be measured at a width it will not
    // be laid out at.
    private MainSlack Distribute(float mainExtent, float crossExtent)
    {
        var totalBasis = 0f;
        var totalGrow = 0f;
        var totalShrink = 0f;
        var count = 0;
        foreach (var child in Children)
        {
            if (!child.IsVisible) continue;
            totalBasis += MainBasis(child, crossExtent);
            totalGrow += GrowOf(child);
            totalShrink += ShrinkOf(child);
            count++;
        }
        if (count == 0) return default;
        return new MainSlack(
            mainExtent - (totalBasis + Gap * (count - 1)), totalGrow, totalShrink, count);
    }

    // A child's unstretched main size. crossExtent is only consulted on the vertical axis, where
    // main size is height-for-width; a row's widths never depend on its height.
    private float MainBasis(View child, float crossExtent) =>
        Vert
            ? ColumnChildBasis(child, ColumnChildWidth(child, crossExtent))
            : child.ClampWidth(child.MeasureWidth());

    // Slack (Remaining > 0) is handed out by grow weight; overflow (Remaining < 0) is taken back by
    // shrink weight. Keeping them on separate factors lets an item do one without the other — a
    // Grow fills and yields, a Shrink only yields, a plain item does neither.
    private static float MainSize(View child, float basis, in MainSlack slack, out bool shrunk)
    {
        shrunk = false;
        if (slack.Remaining > 0f)
        {
            var grow = GrowOf(child);
            if (grow > 0f && slack.TotalGrow > 0f)
                basis += grow / slack.TotalGrow * slack.Remaining;
        }
        else if (slack.Remaining < 0f)
        {
            var shrink = ShrinkOf(child);
            if (shrink > 0f && slack.TotalShrink > 0f)
            {
                basis += shrink / slack.TotalShrink * slack.Remaining;
                if (basis < 0f) basis = 0f;
                shrunk = true;
            }
        }
        return basis;
    }

    private float RowChildWidth(View child, in MainSlack slack, out bool shrunk) =>
        MainSize(child, MainBasis(child, 0f), slack, out shrunk);

    // A row child's height is height-for-width at the width it was just granted — never at its
    // intrinsic width, which for wrapping content is the width of the whole unwrapped run.
    private float RowChildHeight(View child, float crossExtent, float width) =>
        CrossAxisAlignment == CrossAxisAlignment.Stretch ? crossExtent : child.MeasureHeight(width);

    private float ColumnChildWidth(View child, float crossExtent) =>
        CrossAxisAlignment == CrossAxisAlignment.Stretch ? crossExtent : child.ClampWidth(child.MeasureWidth());

    private static float ColumnChildBasis(View child, float width) =>
        child.ClampHeight(child.MeasureHeight(width));

    private float ColumnChildHeight(View child, float width, in MainSlack slack, out bool shrunk) =>
        MainSize(child, ColumnChildBasis(child, width), slack, out shrunk);

    private float CrossPosition(in RectF pos, float finalCross, float crossExtent)
    {
        if (Vert)
        {
            return CrossAxisAlignment switch
            {
                CrossAxisAlignment.End => pos.Right - finalCross,
                CrossAxisAlignment.Center => pos.Left + (crossExtent - finalCross) / 2f,
                _ => pos.Left,
            };
        }

        return CrossAxisAlignment switch
        {
            CrossAxisAlignment.Start => pos.Top - finalCross, // top
            CrossAxisAlignment.End => pos.Bottom,
            CrossAxisAlignment.Center => pos.Bottom + (crossExtent - finalCross) / 2f,
            _ => pos.Bottom,
        };
    }
}
