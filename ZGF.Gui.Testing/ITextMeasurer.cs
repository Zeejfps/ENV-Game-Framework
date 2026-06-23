namespace ZGF.Gui.Testing;

/// <summary>Text metrics seam for <see cref="RecordingCanvas"/>, so layout tests pick between
/// deterministic synthetic metrics and real font shaping.</summary>
public interface ITextMeasurer
{
    float MeasureTextWidth(ReadOnlySpan<char> text, TextStyle style);
    float MeasureTextPrefix(ReadOnlySpan<char> text, int prefixLength, TextStyle style);
    float MeasureTextLineHeight(TextStyle style);
}

/// <summary>Platform-independent metrics: 8px per char, 16px per line. Matches the test
/// <c>FakeCanvas</c> so existing layout expectations hold.</summary>
public sealed class SyntheticTextMeasurer : ITextMeasurer
{
    public float MeasureTextWidth(ReadOnlySpan<char> text, TextStyle style) => text.Length * 8f;

    public float MeasureTextPrefix(ReadOnlySpan<char> text, int prefixLength, TextStyle style) =>
        Math.Clamp(prefixLength, 0, text.Length) * 8f;

    public float MeasureTextLineHeight(TextStyle style) => 16f;
}
