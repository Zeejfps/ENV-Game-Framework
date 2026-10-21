namespace ZGF.Gui.Immediate;

/// <summary>What the core draws with: the window background, default text, spacing and
/// scrollbars. Widget libraries carry their own themes through <see cref="Ui.Provide{T}"/>.</summary>
public sealed record UiStyle
{
    public uint Background { get; init; }
    public uint Text { get; init; }
    public uint ScrollThumb { get; init; }
    public uint ScrollThumbHovered { get; init; }
    public uint ScrollThumbHeld { get; init; }

    public float FontSize { get; init; } = 14f;
    public float Gap { get; init; } = 8f;
    public float WindowPadding { get; init; } = 16f;
    public float ScrollBarWidth { get; init; } = 6f;
    public float ScrollWheelStep { get; init; } = 40f;

    public static UiStyle Dark { get; } = new()
    {
        Background = 0xFF1A1B1E,
        Text = 0xFFE6E7EA,
        ScrollThumb = 0xFF33353B,
        ScrollThumbHovered = 0xFF45484F,
        ScrollThumbHeld = 0xFF8C8F98,
    };
}
