namespace ZGF.Gui.Layouts;

public sealed class FlexItem : View
{
    public StyleValue<float> Grow { get; init; }

    public required MultiChildView Child
    {
        init => AddChildToSelf(value);
    }
}
