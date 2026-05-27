namespace GitGui;

public sealed record ThemeStyles(HeaderActionButtonStyles HeaderActionButton)
{
    public static readonly ThemeStyles Dark = new(
        HeaderActionButton: new HeaderActionButtonStyles(
            Background: 0x00000000u,
            BackgroundHover: 0xFF3A3D43,
            IconIdle: 0xFFB5B9C0,
            IconHover: 0xFFFFFFFFu,
            IconDisabled: 0x66B5B9C0));

    public static readonly ThemeStyles Light = new(
        HeaderActionButton: new HeaderActionButtonStyles(
            Background: 0x00000000u,
            BackgroundHover: 0xFFE5E7EB,
            IconIdle: 0xFF4B5563,
            IconHover: 0xFF111827,
            IconDisabled: 0x664B5563));
}

public sealed record HeaderActionButtonStyles(
    uint Background,
    uint BackgroundHover,
    uint IconIdle,
    uint IconHover,
    uint IconDisabled);
