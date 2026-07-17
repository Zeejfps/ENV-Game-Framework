namespace ZGF.Gui.Views;

public sealed class FlexItem : View
{
    // Settable so a bound Grow (e.g. an animated progress bar) re-lays-out the parent flex on change.
    private StyleValue<float> _grow;
    public StyleValue<float> Grow
    {
        get => _grow;
        set => SetField(ref _grow, value);
    }

    // Weight for giving up space when the container overflows (main-axis size < sum of bases).
    // Independent of Grow so an item can shrink without growing — e.g. an ellipsizing label that
    // yields to a pinned sibling. 0 (the default) means the item holds its basis and never shrinks.
    private StyleValue<float> _shrink;
    public StyleValue<float> Shrink
    {
        get => _shrink;
        set => SetField(ref _shrink, value);
    }

    public required View Child
    {
        init => AddChildToSelf(value);
    }
}
