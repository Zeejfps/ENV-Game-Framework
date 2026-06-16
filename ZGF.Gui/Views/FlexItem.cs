namespace ZGF.Gui.Views;

public sealed class FlexItem : View
{
    public StyleValue<float> Grow { get; init; }

    public required View Child
    {
        init => AddChildToSelf(value);
    }
}
