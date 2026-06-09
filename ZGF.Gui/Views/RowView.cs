namespace ZGF.Gui.Views;

/// <summary>Horizontal stack: a <see cref="FlexView"/> that stretches children across the cross axis. Gap is integral for back-compat.</summary>
public sealed class RowView : FlexView
{
    public RowView()
    {
        Axis = Axis.Horizontal;
        CrossAxisAlignment = CrossAxisAlignment.Stretch;
    }

    public new int Gap
    {
        get => (int)base.Gap;
        set => base.Gap = value;
    }
}
