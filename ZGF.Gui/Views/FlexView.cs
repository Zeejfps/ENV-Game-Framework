using ZGF.Geometry;

namespace ZGF.Gui.Views;

/// <summary>
/// Single axis-parameterized flex container. Replaces the four duplicated V1 containers
/// (Column/Row/FlexColumn/FlexRow): one algorithm written against a main/cross axis abstraction.
/// Children opt into growth by wrapping in a <see cref="FlexItem"/>. Coordinates are still Y-up
/// (top-anchored main advance) until the W1.7 Y-down flip.
/// </summary>
public class FlexView : LayoutView
{
    public Axis Axis { get; init; } = Axis.Vertical;

    private float _gap;
    public float Gap
    {
        get => _gap;
        set => SetField(ref _gap, value);
    }

    private CrossAxisAlignment _crossAxisAlignment;
    public CrossAxisAlignment CrossAxisAlignment
    {
        get => _crossAxisAlignment;
        set => SetField(ref _crossAxisAlignment, value);
    }

    private MainAxisAlignment _mainAxisAlignment;
    public MainAxisAlignment MainAxisAlignment
    {
        get => _mainAxisAlignment;
        set => SetField(ref _mainAxisAlignment, value);
    }

    // Pack (cross, main) into a Constraints in this view's axis orientation.
    private Constraints Make(float minCross, float maxCross, float minMain, float maxMain) =>
        Axis == Axis.Vertical
            ? new Constraints(minCross, maxCross, minMain, maxMain)
            : new Constraints(minMain, maxMain, minCross, maxCross);

    private float Main(Size s) => Axis.Main(s);
    private float Cross(Size s) => Axis.Cross(s);

    private static float GrowOf(View child) => child is FlexItem item ? (float)item.Grow : 0f;

    protected override Size MeasureContent(Constraints c)
    {
        var crossMax = Axis == Axis.Vertical ? c.MaxWidth : c.MaxHeight;

        var mainSum = 0f;
        var crossMax2 = 0f;
        var n = 0;
        foreach (var child in _children)
        {
            if (!child.IsVisible) continue;
            var s = child.Measure(Make(0, crossMax, 0, float.PositiveInfinity));
            mainSum += Main(s);
            crossMax2 = Math.Max(crossMax2, Cross(s));
            n++;
        }

        var main = mainSum + (n > 0 ? _gap * (n - 1) : 0f);
        return Axis.Pack(main, crossMax2);
    }

    protected override void ArrangeContent(RectF bounds)
    {
        var size = new Size(bounds.Width, bounds.Height);
        var mainExtent = Main(size);
        var crossExtent = Cross(size);

        // Pass 1: basis (natural main at the cross extent) + total grow weight.
        var totalBasis = 0f;
        var totalGrow = 0f;
        var n = 0;
        foreach (var child in _children)
        {
            if (!child.IsVisible) continue;
            totalBasis += Main(child.Measure(Make(crossExtent, crossExtent, 0, float.PositiveInfinity)));
            totalGrow += GrowOf(child);
            n++;
        }
        if (n == 0) return;

        var remaining = mainExtent - (totalBasis + _gap * (n - 1));

        var mainOffset = 0f;
        var interItem = 0f;
        if (remaining > 0)
        {
            switch (_mainAxisAlignment)
            {
                case MainAxisAlignment.End: mainOffset = remaining; break;
                case MainAxisAlignment.Center: mainOffset = remaining / 2f; break;
                case MainAxisAlignment.SpaceBetween: interItem = n > 1 ? remaining / (n - 1) : 0f; break;
                case MainAxisAlignment.SpaceAround: interItem = remaining / n; mainOffset = interItem / 2f; break;
                case MainAxisAlignment.SpaceEvenly: interItem = remaining / (n + 1); mainOffset = interItem; break;
            }
        }

        // Y-up: vertical advances downward from the top edge; horizontal rightward from the left.
        var cursor = Axis == Axis.Vertical ? bounds.Top - mainOffset : bounds.Left + mainOffset;

        foreach (var child in _children)
        {
            if (!child.IsVisible) continue;

            float finalCross;
            if (_crossAxisAlignment == CrossAxisAlignment.Stretch)
                finalCross = crossExtent;
            else
            {
                var natural = Cross(child.Measure(Make(0, crossExtent, 0, float.PositiveInfinity)));
                finalCross = Math.Min(natural, crossExtent);
            }

            var finalMain = Main(child.Measure(Make(finalCross, finalCross, 0, float.PositiveInfinity)));
            var grow = GrowOf(child);
            if (grow > 0 && totalGrow > 0)
            {
                finalMain += grow / totalGrow * remaining;
                if (finalMain < 0) finalMain = 0;
            }

            var crossPos = CrossStart(bounds, finalCross, crossExtent);

            RectF rect;
            if (Axis == Axis.Vertical)
                rect = new RectF(crossPos, cursor - finalMain, finalCross, finalMain);
            else
                rect = new RectF(cursor, crossPos, finalMain, finalCross);

            child.Measure(Constraints.Tight(rect.Width, rect.Height));
            child.Arrange(rect);

            cursor = Axis == Axis.Vertical
                ? cursor - finalMain - _gap - interItem
                : cursor + finalMain + _gap + interItem;
        }
    }

    private float CrossStart(RectF bounds, float finalCross, float crossExtent)
    {
        if (Axis == Axis.Vertical)
        {
            // cross = horizontal (Left)
            return _crossAxisAlignment switch
            {
                CrossAxisAlignment.End => bounds.Right - finalCross,
                CrossAxisAlignment.Center => bounds.Left + (crossExtent - finalCross) / 2f,
                _ => bounds.Left, // Start, Stretch
            };
        }

        // cross = vertical (Bottom); Y-up so Start = top, End = bottom
        return _crossAxisAlignment switch
        {
            CrossAxisAlignment.Start => bounds.Top - finalCross,
            CrossAxisAlignment.Center => bounds.Bottom + (crossExtent - finalCross) / 2f,
            _ => bounds.Bottom, // End, Stretch
        };
    }
}
