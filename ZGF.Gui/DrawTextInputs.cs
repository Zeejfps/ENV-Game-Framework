using ZGF.Geometry;

namespace ZGF.Gui;

public readonly struct DrawTextInputs
{
    public required RectF Position { get; init; }
    public required string Text { get; init; }
    public required TextStyle Style {get; init; }
    public required int ZIndex { get; init; }

    /// <summary>The part of <see cref="Text"/> to draw, shaped on its own; all of it when null.</summary>
    public Range? Slice { get; init; }

    public ReadOnlySpan<char> Span => Slice is { } slice ? Text.AsSpan()[slice] : Text.AsSpan();
}