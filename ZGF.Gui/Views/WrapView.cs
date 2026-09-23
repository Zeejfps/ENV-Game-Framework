namespace ZGF.Gui.Views;

/// <summary>
/// A row that breaks onto further runs when its children no longer fit side by side. Within a run,
/// a <see cref="FlexItem"/> with grow takes the run's leftover width, so a spacer still pushes what
/// follows it to the end of its run. Runs are measured and laid out by the same split, so the height
/// reported for a width is the height drawn at it.
/// </summary>
public sealed class WrapView : View
{
    public new ChildrenCollection Children => base.Children;

    public float Gap
    {
        get;
        set => SetField(ref field, value);
    }

    public float RunGap
    {
        get;
        set => SetField(ref field, value);
    }

    private readonly record struct Placed(View Child, float Width);

    protected override float MeasureWidthIntrinsic()
    {
        if (Width.IsSet) return Width;
        var total = 0f;
        var count = 0;
        foreach (var child in Children)
        {
            if (!child.IsVisible) continue;
            total += child.MeasureWidth();
            count++;
        }
        return total + (count > 0 ? (count - 1) * Gap : 0f);
    }

    protected override float MeasureHeightIntrinsic(float availableWidth)
    {
        if (Height.IsSet) return Height;
        var runs = Runs(availableWidth);
        var height = 0f;
        foreach (var run in runs)
            height += RunHeight(run);
        return height + (runs.Count > 0 ? (runs.Count - 1) * RunGap : 0f);
    }

    protected override void OnLayoutChildren()
    {
        var pos = Position;
        var rtl = IsRtl;
        var top = pos.Top;
        foreach (var run in Runs(pos.Width))
        {
            var height = RunHeight(run);
            var cursor = pos.Left;
            foreach (var (child, width) in run)
            {
                var childHeight = child.MeasureHeight(width);
                child.LeftConstraint = rtl ? pos.Left + pos.Right - cursor - width : cursor;
                child.BottomConstraint = top - (height + childHeight) / 2f;
                child.WidthConstraint = width;
                child.HeightConstraint = childHeight;
                child.LayoutSelf();
                cursor += width + Gap;
            }
            top -= height + RunGap;
        }
    }

    private static float RunHeight(List<Placed> run)
    {
        var height = 0f;
        foreach (var (child, width) in run)
            height = MathF.Max(height, child.MeasureHeight(width));
        return height;
    }

    // Non-positive width is unconstrained: everything goes on one run.
    private List<List<Placed>> Runs(float width)
    {
        var unconstrained = width <= 0f;
        var runs = new List<List<Placed>>();
        List<Placed>? run = null;
        var used = 0f;
        foreach (var child in Children)
        {
            if (!child.IsVisible) continue;
            var childWidth = child.MeasureWidth();
            if (!unconstrained) childWidth = MathF.Min(childWidth, width);
            if (run is null || (!unconstrained && used + Gap + childWidth > width))
            {
                if (run is not null) Grow(run, width - used);
                run = [];
                runs.Add(run);
                used = childWidth;
            }
            else
            {
                used += Gap + childWidth;
            }
            run.Add(new Placed(child, childWidth));
        }
        if (run is not null && !unconstrained) Grow(run, width - used);
        return runs;
    }

    private static void Grow(List<Placed> run, float slack)
    {
        if (slack <= 0f) return;
        var total = 0f;
        foreach (var placed in run)
            if (placed.Child is FlexItem item) total += item.Grow;
        if (total <= 0f) return;
        for (var i = 0; i < run.Count; i++)
            if (run[i].Child is FlexItem item && item.Grow > 0f)
                run[i] = run[i] with { Width = run[i].Width + item.Grow / total * slack };
    }
}
