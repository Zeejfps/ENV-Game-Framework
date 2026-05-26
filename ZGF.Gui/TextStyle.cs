namespace ZGF.Gui;

/// <summary>
/// Snapshot of the text-drawing parameters a <see cref="DrawTextInputs"/> needs. Passed
/// by value — fields are immutable; produce a derived instance with the <c>with</c>
/// expression: <c>var styled = baseStyle with { TextColor = newColor };</c>.
/// </summary>
public readonly record struct TextStyle
{
    public TextStyle() { }

    public StyleValue<uint> TextColor { get; init; } = new(0xFF000000, false);
    public StyleValue<string> FontFamily { get; init; }
    public StyleValue<float> FontSize { get; init; }
    public StyleValue<FontWeight> FontWeight { get; init; }
    public StyleValue<TextAlignment> HorizontalAlignment { get; init; }
    public StyleValue<TextAlignment> VerticalAlignment { get; init; }
    public StyleValue<TextWrap> TextWrap { get; init; }
    public StyleValue<float> Rotation { get; init; } = new(0f, false);
}
