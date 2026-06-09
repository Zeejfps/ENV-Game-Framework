using ZGF.Geometry;

namespace ZGF.Gui.Views;

/// <summary>
/// Insets its children by a <see cref="PaddingStyle"/>. No background, border, or
/// drawing — use this for pure spacing. Reach for <see cref="RectView"/> only when
/// you also need to paint a box.
/// </summary>
public sealed class PaddingView : LayoutView
{
    private PaddingStyle _padding;

    public PaddingStyle Padding
    {
        get => _padding;
        set => SetField(ref _padding, value);
    }

    protected override Size MeasureContent(Constraints c)
    {
        var hPad = _padding.Left + _padding.Right;
        var vPad = _padding.Top + _padding.Bottom;
        var inner = c.Deflate(hPad, vPad);

        var w = 0f;
        var h = 0f;
        foreach (var child in _children)
        {
            if (!child.IsVisible) continue;
            var s = child.Measure(inner);
            if (s.Width > w) w = s.Width;
            if (s.Height > h) h = s.Height;
        }
        return new Size(w + hPad, h + vPad);
    }

    protected override void ArrangeContent(RectF bounds)
    {
        var left = bounds.Left + _padding.Left;
        var bottom = bounds.Bottom + _padding.Bottom;
        var width = Math.Max(0f, bounds.Width - _padding.Left - _padding.Right);
        var height = Math.Max(0f, bounds.Height - _padding.Top - _padding.Bottom);
        var inner = new RectF(left, bottom, width, height);

        foreach (var child in _children)
        {
            if (!child.IsVisible) continue;
            child.Measure(Constraints.Tight(width, height));
            child.Arrange(inner);
        }
    }
}
