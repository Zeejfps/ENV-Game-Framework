namespace ZGF.Gui.Views;

public sealed class RowView : MultiChildView
{
    private int _gap;
    public int Gap
    {
        get => _gap;
        set => SetField(ref _gap, value);
    }

    protected override void OnLayoutChildren()
    {
        var position = Position;
        var components = Children;
        if (components.Count == 0)
        {
            return;
        }

        var left = position.Left;
        var first = true;
        foreach (var component in components)
        {
            if (!component.IsVisible) continue;
            if (!first) left += Gap;
            component.LeftConstraint = left;
            component.BottomConstraint = position.Bottom;
            component.HeightConstraint = position.Height;
            component.LayoutSelf();
            left += component.MeasureWidth();
            first = false;
        }
    }

    public override float MeasureWidth()
    {
        if (Width.IsSet)
            return Width;

        var totalWidth = 0f;
        var visibleCount = 0;
        foreach (var child in Children)
        {
            if (!child.IsVisible) continue;
            totalWidth += child.MeasureWidth();
            visibleCount++;
        }
        var spacing = visibleCount > 0 ? (visibleCount - 1) * Gap : 0;

        return totalWidth + spacing;
    }
}