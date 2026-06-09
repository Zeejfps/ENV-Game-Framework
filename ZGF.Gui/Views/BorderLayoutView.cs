using ZGF.Geometry;

namespace ZGF.Gui.Views;

public sealed class BorderLayoutView : View
{
    public View? North
    {
        get;
        set => SetView(ref field, value);
    }

    public View? East
    {
        get;
        set => SetView(ref field, value);
    }

    public View? West
    {
        get;
        set => SetView(ref field, value);
    }

    private View? _south;
    public View? South
    {
        get => _south;
        set => SetView(ref _south, value);
    }

    public View? Center
    {
        get;
        set => SetView(ref field, value);
    }

    private void SetView(ref View? view, View? value)
    {
        if (view == value)
            return;

        var prevComponent = view;
        view = value;

        if (prevComponent != null)
        {
            RemoveChildFromSelf(prevComponent);
        }

        if (view != null)
        {
            AddChildToSelf(view);
        }
    }

    public override float MeasureWidth() => Measure(Constraints.Unbounded).Width;

    public override float MeasureHeight(float availableWidth)
    {
        var maxW = availableWidth > 0f ? availableWidth : float.PositiveInfinity;
        return Measure(new Constraints(0, maxW, 0, float.PositiveInfinity)).Height;
    }

    protected override void OnLayoutChildren() => ArrangeContent(Position);

    protected override Size MeasureContent(Constraints c)
    {
        var inf = float.PositiveInfinity;
        var northH = North?.Measure(new Constraints(0, c.MaxWidth, 0, inf)).Height ?? 0f;
        var southH = South?.Measure(new Constraints(0, c.MaxWidth, 0, inf)).Height ?? 0f;

        var west = West?.Measure(Constraints.Unbounded) ?? new Size(0, 0);
        var east = East?.Measure(Constraints.Unbounded) ?? new Size(0, 0);
        var center = Center?.Measure(Constraints.Unbounded) ?? new Size(0, 0);

        var bandHeight = Math.Max(center.Height, Math.Max(west.Height, east.Height));
        var width = Math.Max(west.Width + center.Width + east.Width, 0f);
        return new Size(width, northH + southH + bandHeight);
    }

    protected override void ArrangeContent(RectF bounds)
    {
        var centerAreaWidth = bounds.Width;
        var centerAreaHeight = bounds.Height;
        var leftOffset = 0f;
        var bottomOffset = 0f;

        if (North != null)
        {
            var height = North.Measure(new Constraints(bounds.Width, bounds.Width, 0, float.PositiveInfinity)).Height;
            North.Arrange(new RectF(bounds.Left, bounds.Top - height, bounds.Width, height));
            centerAreaHeight -= height;
        }

        if (South != null)
        {
            var height = South.Measure(new Constraints(bounds.Width, bounds.Width, 0, float.PositiveInfinity)).Height;
            South.Arrange(new RectF(bounds.Left, bounds.Bottom, bounds.Width, height));
            centerAreaHeight -= height;
            bottomOffset += height;
        }

        if (West != null)
        {
            var width = West.Measure(new Constraints(0, bounds.Width, centerAreaHeight, centerAreaHeight)).Width;
            West.Arrange(new RectF(bounds.Left, bounds.Bottom + bottomOffset, width, centerAreaHeight));
            centerAreaWidth -= width;
            leftOffset += width;
        }

        if (East != null)
        {
            var width = East.Measure(new Constraints(0, bounds.Width, centerAreaHeight, centerAreaHeight)).Width;
            East.Arrange(new RectF(bounds.Right - width, bounds.Bottom + bottomOffset, width, centerAreaHeight));
            centerAreaWidth -= width;
        }

        if (Center != null)
        {
            var w = Math.Max(0f, centerAreaWidth);
            var h = Math.Max(0f, centerAreaHeight);
            Center.Arrange(new RectF(bounds.Left + leftOffset, bounds.Bottom + bottomOffset, w, h));
        }
    }
}
