namespace GitGui;

public sealed record ThemeStyles(
    HeaderActionButtonStyles HeaderActionButton,
    LocalChangesContentStyles LocalChangesContent,
    SubmoduleSectionStyles SubmoduleSection,
    FileChangesSectionStyles FileChangesSection,
    FileChangeRowStyles FileChangeRow)
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
            EmptyPlaceholderText: 0xFF96989Du),
        FileChangeRow: new FileChangeRowStyles(
            RowText: 0xFFB5B9C0u,
            RowTextActive: 0xFFFFFFFFu,
            RowHover: 0xFF2B2D31u,
            RowActive: 0xFF404C8Cu,
            BadgeText: 0xFF1A1B1Eu,
            StatusAdded: 0xFF57F287u,
            StatusModified: 0xFFE9C77Au,
            StatusDeleted: 0xFFED4245u,
            StatusRenamed: 0xFF5DADE2u,
            StatusConflicted: 0xFFED4245u,
            StatusSubmodule: 0xFFB57EDCu,
            StatusOther: 0xFF9B59B6u));

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
            EmptyPlaceholderText: 0xFF6B7280u),
        FileChangeRow: new FileChangeRowStyles(
            RowText: 0xFF374151u,
            RowTextActive: 0xFFFFFFFFu,
            RowHover: 0xFFF3F4F6u,
            RowActive: 0xFF4F46E5u,
            BadgeText: 0xFFFFFFFFu,
            StatusAdded: 0xFF16A34Au,
            StatusModified: 0xFFCA8A04u,
            StatusDeleted: 0xFFDC2626u,
            StatusRenamed: 0xFF2563EBu,
            StatusConflicted: 0xFFDC2626u,
            StatusSubmodule: 0xFFA855F7u,
            StatusOther: 0xFF7C3AEDu));
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

public sealed record FileChangeRowStyles(
    uint RowText,
    uint RowTextActive,
    uint RowHover,
    uint RowActive,
    uint BadgeText,
    uint StatusAdded,
    uint StatusModified,
    uint StatusDeleted,
    uint StatusRenamed,
    uint StatusConflicted,
    uint StatusSubmodule,
    uint StatusOther)
{
    public uint StatusColor(FileChangeStatus status) => status switch
    {
        FileChangeStatus.Added => StatusAdded,
        FileChangeStatus.Modified => StatusModified,
        FileChangeStatus.Deleted => StatusDeleted,
        FileChangeStatus.Renamed => StatusRenamed,
        FileChangeStatus.Conflicted => StatusConflicted,
        FileChangeStatus.Submodule => StatusSubmodule,
        _ => StatusOther,
    };
}
