namespace ZGF.Gui.Views;

/// <summary>Horizontal stack that stretches children across the cross axis. Integral gap for back-compat.</summary>
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
