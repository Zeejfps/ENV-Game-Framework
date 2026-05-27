namespace GitGui;

public sealed record ThemeStyles(
    HeaderActionButtonStyles HeaderActionButton,
    LocalChangesContentStyles LocalChangesContent,
    SubmoduleSectionStyles SubmoduleSection,
    FileChangesSectionStyles FileChangesSection)
{
    public static readonly ThemeStyles Dark = new(
        HeaderActionButton: new HeaderActionButtonStyles(
            Background: 0x00000000u,
            BackgroundHover: 0xFF3A3D43,
            IconIdle: 0xFFB5B9C0,
            IconHover: 0xFFFFFFFFu,
            IconDisabled: 0x66B5B9C0),
        LocalChangesContent: new LocalChangesContentStyles(
            ContentBackground: 0xFF1E1F22u,
            ColumnDivider: 0xFF313338u,
            PlaceholderText: 0xFF96989Du,
            SplitterIdle: 0xFF313338u,
            SplitterHover: 0xFF4A5680u),
        SubmoduleSection: new SubmoduleSectionStyles(
            BadgeBackground: 0xFFB57EDCu,
            BadgeText: 0xFF1A1B1Eu,
            RowText: 0xFFB5B9C0u),
        FileChangesSection: new FileChangesSectionStyles(
            HeaderBackground: 0xFF222326u,
            HeaderBorder: 0xFF313338u,
            HeaderText: 0xFF96989Du,
            EmptyPlaceholderText: 0xFF96989Du));

    public static readonly ThemeStyles Light = new(
        HeaderActionButton: new HeaderActionButtonStyles(
            Background: 0x00000000u,
            BackgroundHover: 0xFFE5E7EB,
            IconIdle: 0xFF4B5563,
            IconHover: 0xFF111827,
            IconDisabled: 0x664B5563),
        LocalChangesContent: new LocalChangesContentStyles(
            ContentBackground: 0xFFFFFFFFu,
            ColumnDivider: 0xFFE5E7EBu,
            PlaceholderText: 0xFF6B7280u,
            SplitterIdle: 0xFFE5E7EBu,
            SplitterHover: 0xFFCBD5E1u),
        SubmoduleSection: new SubmoduleSectionStyles(
            BadgeBackground: 0xFFA855F7u,
            BadgeText: 0xFFFFFFFFu,
            RowText: 0xFF4B5563u),
        FileChangesSection: new FileChangesSectionStyles(
            HeaderBackground: 0xFFF3F4F6u,
            HeaderBorder: 0xFFE5E7EBu,
            HeaderText: 0xFF6B7280u,
            EmptyPlaceholderText: 0xFF6B7280u));
}

public sealed record HeaderActionButtonStyles(
    uint Background,
    uint BackgroundHover,
    uint IconIdle,
    uint IconHover,
    uint IconDisabled);

public sealed record LocalChangesContentStyles(
    uint ContentBackground,
    uint ColumnDivider,
    uint PlaceholderText,
    uint SplitterIdle,
    uint SplitterHover);

public sealed record SubmoduleSectionStyles(
    uint BadgeBackground,
    uint BadgeText,
    uint RowText);

public sealed record FileChangesSectionStyles(
    uint HeaderBackground,
    uint HeaderBorder,
    uint HeaderText,
    uint EmptyPlaceholderText);
