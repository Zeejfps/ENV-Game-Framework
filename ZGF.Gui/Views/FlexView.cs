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

    /// <summary>
    /// Right-to-left layout: horizontal placement (a Row's main axis, a Column's cross axis) is
    /// mirrored within the container so Start lands on the right and child order reverses. Vertical
    /// placement is untouched.
    /// </summary>
    public bool IsRtl
    {
        get;
        set => SetField(ref field, value);
    }

    private bool Vert => Axis == Axis.Vertical;

    protected override float MeasureWidthIntrinsic()
    {
        if (Width.IsSet) return Width;
        if (Vert) return MeasureChildrenWidth();
        return SumMain(0f);
    }

    protected override float MeasureHeightIntrinsic(float availableWidth)
    {
        if (Height.IsSet) return Height;
        if (!Vert) return MeasureChildrenHeight(availableWidth);
        return SumMain(availableWidth);
    }

    // Sum of the main-axis size of visible children plus gaps. availableWidth is only
    // consulted on the vertical axis, where main size is height-for-width.
    private float SumMain(float availableWidth)
    {
        var total = 0f;
        var count = 0;
        foreach (var child in Children)
        {
            if (!child.IsVisible) continue;
            total += Vert ? child.MeasureHeight(availableWidth) : child.MeasureWidth();
            count++;
        }
        return total + (count > 0 ? (count - 1) * Gap : 0f);
    }

    protected override void OnLayoutChildren()
    {
        var pos = Position;
        if (Children.Count == 0) return;

        var mainExtent = Vert ? pos.Height : pos.Width;
        var crossExtent = Vert ? pos.Width : pos.Height;

        var totalBasis = 0f;
        var totalGrow = 0f;
        var visibleCount = 0;
        foreach (var child in Children)
        {
            if (!child.IsVisible) continue;
            totalBasis += MainBasis(child, crossExtent);
            totalGrow += GrowOf(child);
            visibleCount++;
        }
        if (visibleCount == 0) return;

        var remaining = mainExtent - (totalBasis + Gap * (visibleCount - 1));

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

            float finalCross, crossPos;
            if (CrossAxisAlignment == CrossAxisAlignment.Stretch)
            {
                finalCross = crossExtent;
                crossPos = Vert ? pos.Left : pos.Bottom;
            }
            else
            {
                finalCross = CrossNatural(child);
                crossPos = CrossPosition(pos, finalCross, crossExtent);
            }

            var finalMain = MainFinal(child, finalCross);
            if (grow > 0 && totalGrow > 0)
            {
                finalMain += grow / totalGrow * remaining;
                if (finalMain < 0) finalMain = 0;
            }

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
            if (IsRtl)
                child.LeftConstraint = pos.Left + pos.Right - child.LeftConstraint - child.WidthConstraint;

            if (grow > 0)
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

    private float MainBasis(View child, float crossExtent) =>
        Vert ? child.ClampHeight(child.MeasureHeight(crossExtent)) : child.ClampWidth(child.MeasureWidth());

    private float MainFinal(View child, float finalCross) =>
        Vert ? child.ClampHeight(child.MeasureHeight(finalCross)) : child.ClampWidth(child.MeasureWidth());

    private float CrossNatural(View child) =>
        Vert ? child.ClampWidth(child.MeasureWidth()) : child.ClampHeight(child.MeasureHeight(child.MeasureWidth()));

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
