using ZGF.Fonts;

namespace ZGF.Gui;

public sealed record TextStyle
{
    public StyleValue<uint> TextColor = new(0xFF000000, false);
    public StyleValue<string> FontFamily;
    public StyleValue<float> FontSize;
    public StyleValue<FontWeight> FontWeight;
    public StyleValue<TextAlignment> HorizontalAlignment;
    public StyleValue<TextAlignment> VerticalAlignment;
    public StyleValue<TextWrap> TextWrap;
    public StyleValue<TextOverflow> TextOverflow;
    public StyleValue<float> Rotation = new(0f, false);
    public StyleValue<FontFeatureSet> FontFeatures;
    // Base paragraph direction for bidi reordering and Start/End alignment. Unset defers to the
    // canvas's DefaultBaseDirection; set it to force a direction on direction-neutral content (an
    // LTR SHA/path in an otherwise RTL UI).
    public StyleValue<BidiDirection> BaseDirection;
}
