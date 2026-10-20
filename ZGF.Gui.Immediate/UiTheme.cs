namespace ZGF.Gui.Immediate;

public sealed record UiTheme
{
    public uint Background { get; init; }
    public uint Panel { get; init; }
    public uint PanelBorder { get; init; }
    public uint Text { get; init; }
    public uint TextMuted { get; init; }
    public uint Control { get; init; }
    public uint ControlHovered { get; init; }
    public uint ControlActive { get; init; }
    public uint ControlBorder { get; init; }
    public uint FieldBackground { get; init; }
    public uint Accent { get; init; }
    public uint OnAccent { get; init; }
    public uint Selection { get; init; }

    public float FontSize { get; init; } = 14f;
    public float ControlHeight { get; init; } = 30f;
    public float ControlPaddingX { get; init; } = 12f;
    public float CornerRadius { get; init; } = 6f;
    public float Gap { get; init; } = 8f;
    public float WindowPadding { get; init; } = 16f;
    public float PanelPadding { get; init; } = 12f;
    public float ScrollBarWidth { get; init; } = 6f;
    public float ScrollWheelStep { get; init; } = 40f;

    public static UiTheme Dark { get; } = new()
    {
        Background = 0xFF1A1B1E,
        Panel = 0xFF232428,
        PanelBorder = 0xFF34363C,
        Text = 0xFFE6E7EA,
        TextMuted = 0xFF8C8F98,
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
