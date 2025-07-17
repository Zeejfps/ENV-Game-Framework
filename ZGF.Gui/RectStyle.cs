using ZGF.Geometry;

namespace ZGF.Gui;

public sealed class RectStyle
{
    public StyleValue<uint> BackgroundColor;
    public PaddingStyle Padding;
    public BorderColorStyle BorderColor;
    public BorderSizeStyle BorderSize;

    public void Apply(Style style)
    {
        if (style.BackgroundColor.IsSet)
            BackgroundColor = style.BackgroundColor.Value;
            
        style.Padding.ApplyTo(ref Padding);
        style.BorderSize.ApplyTo(ref BorderSize);
        style.BorderColor.ApplyTo(ref BorderColor);
    }
}

public readonly struct DrawRectCommand
{
    public required RectF Position { get; init; }
    public required RectStyle Style {get; init; }
    public required int ZIndex { get; init; }
}

public readonly struct DrawTextCommand
{
    public required RectF Position { get; init; }
    public required string Text { get; init; }
    public required TextStyle Style {get; init; }
    public required int ZIndex { get; init; }
}