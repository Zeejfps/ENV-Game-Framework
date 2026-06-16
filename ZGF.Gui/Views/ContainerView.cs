namespace ZGF.Gui.Views;

/// <summary>
/// A neutral, freely-populatable container: no drawing, no chrome, default layout.
/// The general-purpose grouping view — slot-style views (e.g. <see cref="BorderLayoutView"/>,
/// <see cref="FlexItem"/>) keep their children protected and accept content only through
/// their slots.
/// </summary>
public class ContainerView : View
{
    public new ChildrenCollection Children => base.Children;
}
