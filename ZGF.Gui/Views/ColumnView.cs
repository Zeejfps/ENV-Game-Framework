namespace ZGF.Gui.Views;

/// <summary>Vertical stack: a <see cref="FlexView"/> that stretches children across the cross axis. Gap is integral for back-compat.</summary>
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
