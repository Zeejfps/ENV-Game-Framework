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

    public required View Child
    {
        init => AddChildToSelf(value);
    }
}
