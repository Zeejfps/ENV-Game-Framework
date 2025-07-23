namespace ZGF.Gui.Layouts;

public sealed class RowView : View
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
        var componentCount = components.Count;
        if (componentCount == 0)
        {
            return;
        }

        var left = position.Left;
        foreach (var component in components)
        {
            component.LeftConstraint = left;
            component.BottomConstraint = position.Bottom;
            component.MaxHeightConstraint = position.Height;
            component.LayoutSelf();
            left += component.MeasureWidth() + Gap;
        }
    }

    public override float MeasureWidth()
    {
        if (PreferredWidth.IsSet)
            return PreferredWidth;

        var totalWidth = 0f;
        foreach (var child in Children)
        {
            totalWidth += child.MeasureWidth();
        }
        var spacing = (Children.Count - 1) * Gap;

        return totalWidth + spacing;
    }
}