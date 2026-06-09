namespace ZGF.Gui.Views;

/// <summary>Vertical stack that stretches children across the cross axis. Integral gap for back-compat.</summary>
public sealed class ColumnView : FlexView
{
    public ColumnView()
    {
        CrossAxisAlignment = CrossAxisAlignment.Stretch;
    }

    public new int Gap
    {
        get => (int)base.Gap;
        set => base.Gap = value;
    }
}
