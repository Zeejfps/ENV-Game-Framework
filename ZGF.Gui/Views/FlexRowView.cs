namespace ZGF.Gui.Views;

/// <summary>Horizontal <see cref="FlexView"/>; cross-axis defaults to Start. Grow children via <see cref="FlexItem"/>.</summary>
public sealed class FlexRowView : FlexView
{
    public FlexRowView()
    {
        Axis = Axis.Horizontal;
    }
}
