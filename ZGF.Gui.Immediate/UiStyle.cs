namespace ZGF.Gui.Immediate;

/// <summary>
/// The palette and metrics a UI is drawn with. Colors are named by role, not by widget; hover,
/// pressed, selection and scrollbar tones are derived from them, so a theme is just the roles.
/// The app picks the window's style each frame with <c>ui.UseStyle(style)</c>, and a subtree can
/// use another with <c>ui.Provide(style)</c>.
/// </summary>
public sealed record UiStyle
{
    public uint Background { get; init; }
    public uint Surface { get; init; }
    public uint Control { get; init; }
    public uint Field { get; init; }
    public uint Border { get; init; }
    public uint Text { get; init; }
    public uint TextMuted { get; init; }
    public uint Accent { get; init; }
    public uint OnAccent { get; init; }

    public float FontSize { get; init; } = 14f;
    public float Gap { get; init; } = 8f;
    public float WindowPadding { get; init; } = 16f;
    public float ScrollBarWidth { get; init; } = 6f;
    public float ScrollWheelStep { get; init; } = 40f;

    public uint Selection => UiColor.Mix(Field, Accent, 0.35f);
    public uint ScrollThumb => UiColor.Mix(Background, Text, 0.15f);
    public uint ScrollThumbHovered => UiColor.Mix(Background, Text, 0.25f);
    public uint ScrollThumbHeld => UiColor.Mix(Background, Text, 0.5f);

    public uint Hovered(uint color) => UiColor.Mix(color, Text, 0.06f);

    public uint Pressed(uint color) => UiColor.Mix(color, Text, 0.12f);

    public static UiStyle Dark { get; } = new()
    {
        Background = 0xFF1A1B1E,
        Surface = 0xFF232428,
        Control = 0xFF33353B,
        Field = 0xFF17181B,
        Border = 0xFF3E4046,
        Text = 0xFFE6E7EA,
        TextMuted = 0xFF8C8F98,
        Accent = 0xFF4C8DFF,
        OnAccent = 0xFFFFFFFF,
    };

    public static UiStyle Light { get; } = new()
    {
        Background = 0xFFF4F5F7,
        Surface = 0xFFFFFFFF,
        Control = 0xFFEBECEF,
        Field = 0xFFFFFFFF,
        Border = 0xFFD5D7DC,
        Text = 0xFF1F2328,
        TextMuted = 0xFF6B7079,
        Accent = 0xFF2F6FEB,
        OnAccent = 0xFFFFFFFF,
    };
}
