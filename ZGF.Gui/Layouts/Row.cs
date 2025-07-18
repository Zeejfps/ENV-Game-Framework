namespace ZGF.Gui.Layouts;

public sealed class Row : Component
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

        var spacing = (componentCount - 1) * Gap;
        var totalWidth = position.Width - spacing;
        var componentWidth = totalWidth / componentCount;
        var left = position.Left;
        foreach (var component in components)
        {
            component.LeftConstraint = left;
            component.MinWidthConstraint = componentWidth;
            component.MaxWidthConstraint = componentWidth;
            component.BottomConstraint = position.Bottom;
            component.MaxHeightConstraint = position.Height;
            component.LayoutSelf();
            left += componentWidth + Gap;
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