namespace ZGF.Gui.Immediate.Widgets;

/// <summary>Colors and metrics for the built-in widgets. Text color and font size come from the
/// core <see cref="UiStyle"/>. Give a whole UI or a subtree its own theme with
/// <c>ui.Provide(theme)</c>.</summary>
public sealed record WidgetTheme
{
    public uint TextMuted { get; init; }
    public uint Panel { get; init; }
    public uint PanelBorder { get; init; }
    public uint Control { get; init; }
    public uint ControlHovered { get; init; }
    public uint ControlActive { get; init; }
    public uint ControlBorder { get; init; }
    public uint FieldBackground { get; init; }
    public uint Accent { get; init; }
    public uint OnAccent { get; init; }
    public uint Selection { get; init; }

    public float ControlHeight { get; init; } = 30f;
    public float ControlPaddingX { get; init; } = 12f;
    public float CornerRadius { get; init; } = 6f;
    public float PanelPadding { get; init; } = 12f;

    /// <summary>The theme provided around this point in <paramref name="ui"/>, or
    /// <see cref="Dark"/>.</summary>
    public static WidgetTheme For(Ui ui) => ui.Get<WidgetTheme>() ?? Dark;

    public static WidgetTheme Dark { get; } = new()
    {
        TextMuted = 0xFF8C8F98,
        Panel = 0xFF232428,
        PanelBorder = 0xFF34363C,
        Control = 0xFF33353B,
        ControlHovered = 0xFF3D4047,
        ControlActive = 0xFF2B2D32,
        ControlBorder = 0xFF45484F,
        FieldBackground = 0xFF17181B,
        Accent = 0xFF4C8DFF,
        OnAccent = 0xFFFFFFFF,
        Selection = 0xFF2B4C80,
    };
}
