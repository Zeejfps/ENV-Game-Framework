namespace ZGF.Gui.Layouts;

public sealed class FlexItem : MultiChildView
{
    public StyleValue<float> Grow { get; init; }

    public required MultiChildView Child
    {
        init => AddChildToSelf(value);
    }
}
