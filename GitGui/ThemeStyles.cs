namespace GitGui;

public sealed record ThemeStyles(
    HeaderActionButtonStyles HeaderActionButton,
    LocalChangesContentStyles LocalChangesContent,
    SubmoduleSectionStyles SubmoduleSection,
    FileChangesSectionStyles FileChangesSection,
    FileChangeRowStyles FileChangeRow,
    DialogFrameStyles DialogFrame,
    TextInputStyles TextInput,
    BorderedButtonStyles BorderedButton,
    DialogIconButtonStyles DialogIconButton,
    ActionButtonStyles ActionButton,
    CheckboxStyles Checkbox,
    CommitBarStyles CommitBar,
    ErrorBarStyles ErrorBar,
    ModeSwitcherStyles ModeSwitcher,
    BranchesHeaderStyles BranchesHeader,
    AddRepoButtonStyles AddRepoButton,
    GroupHeaderRowStyles GroupHeaderRow,
    GroupRenameFieldStyles GroupRenameField,
    WorktreeChevronStyles WorktreeChevron,
    RepoBarRowStyles RepoBarRow,
    BranchesViewStyles BranchesView,
    RepoBarStyles RepoBar,
    DiffViewStyles DiffView,
    DiffContentStyles DiffContent,
    DiffHunkButtonStyles DiffHunkButton,
    ActionsToolbarStyles ActionsToolbar,
    SeparatorSpacerStyles SeparatorSpacer,
    SidebarSplitterStyles SidebarSplitter,
    HistorySplitterStyles HistorySplitter,
    ScrollBarStyles ScrollBar,
    LocalChangesViewStyles LocalChangesView,
    TooltipStyles Tooltip,
    CommitsTruncationBarStyles CommitsTruncationBar,
    OperationBannerStyles OperationBanner,
    OperationRowStyles OperationRow)
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
            StatusOther: 0xFF9B59B6u),
        DialogFrame: new DialogFrameStyles(
            Background: 0xFF1E1F22u,
            Border: 0xFF313338u,
            TitleText: 0xFFE6E6E6u,
            HeaderSeparator: 0xFF2A2C30u,
            ErrorText: 0xFFE06C75u),
        TextInput: new TextInputStyles(
            Background: 0xFF2B2D31u,
            Border: 0xFF3E4047u,
            Text: 0xFFE6E6E6u,
            Caret: 0xFFE6E6E6u,
            Selection: 0xFF404C8Cu,
            PlaceholderText: 0x80B5B9C0u),
        BorderedButton: new BorderedButtonStyles(
            BackgroundIdle: 0xFF2B2D31u,
            BackgroundHover: 0xFF3A3D43u,
            BorderIdle: 0xFF3E4047u,
            BorderHover: 0xFF5865F2u,
            Text: 0xFFFFFFFFu,
            TextDisabled: 0x80B5B9C0u),
        DialogIconButton: new DialogIconButtonStyles(
            BackgroundIdle: 0x00000000u,
            BackgroundHover: 0xFF3A3D43u,
            TextIdle: 0xFFB5B9C0u,
            TextHover: 0xFFFFFFFFu),
        ActionButton: new ActionButtonStyles(
            BackgroundIdle: 0x00000000u,
            BackgroundHover: 0xFF3A3D43u,
            TextIdle: 0xFFB5B9C0u,
            TextHover: 0xFFFFFFFFu,
            TextDisabled: 0x80B5B9C0u),
        Checkbox: new CheckboxStyles(
            TextIdle: 0xFFB5B9C0u,
            TextHover: 0xFFFFFFFFu,
            TextDisabled: 0x80B5B9C0u),
        CommitBar: new CommitBarStyles(
            Background: 0xFF2A2C30u,
            TopBorder: 0xFF313338u),
        ErrorBar: new ErrorBarStyles(
            Background: 0xFF3D2E14u,
            Border: 0xFFB89050u,
            Text: 0xFFE9C77Au),
        ModeSwitcher: new ModeSwitcherStyles(
            PillBorder: 0xFF3E4047u,
            SegmentSeparator: 0xFF3E4047u,
            SegmentIdleBackground: 0x00000000u,
            SegmentHoverBackground: 0xFF3A3D43u,
            SegmentActiveBackground: 0xFF404C8Cu,
            SegmentIdleText: 0xFFB5B9C0u,
            SegmentActiveText: 0xFFFFFFFFu),
        BranchesHeader: new BranchesHeaderStyles(
            Background: 0xFF1E1F22u,
            BorderBottom: 0xFF313338u,
            PrefixText: 0xFF96989Du,
            ActiveText: 0xFFFFFFFFu,
            DetachedText: 0x80B5B9C0u),
        AddRepoButton: new AddRepoButtonStyles(
            Text: 0xFFB5B9C0u),
        GroupHeaderRow: new GroupHeaderRowStyles(
            ChevronText: 0xFF96989Du,
            BackgroundIdle: 0x00000000u,
            BackgroundHover: 0xFF2B2D31u,
            NameText: 0xFF96989Du),
        GroupRenameField: new GroupRenameFieldStyles(
            Background: 0xFF2B2D31u,
            Border: 0xFF5865F2u,
            Text: 0xFFE6E6E6u,
            Caret: 0xFFE6E6E6u,
            Selection: 0xFF404C8Cu),
        WorktreeChevron: new WorktreeChevronStyles(
            Text: 0xFFB5B9C0u),
        RepoBarRow: new RepoBarRowStyles(
            BackgroundIdle: 0x00000000u,
            BackgroundHover: 0xFF2B2D31u,
            BackgroundActive: 0xFF404C8Cu,
            TextIdle: 0xFFB5B9C0u,
            TextActive: 0xFFFFFFFFu,
            TextMissing: 0x80B5B9C0u,
            IconAccentWorktree: 0xFF5DADE2u,
            IconAccentSubmodule: 0xFFB57EDCu),
        BranchesView: new BranchesViewStyles(
            ViewBackground: 0xFF1E1F22u,
            RowSelectedBackground: 0xFF404C8Cu,
            RowHoverBackground: 0xFF2B2D31u,
            RowText: 0xFFB5B9C0u,
            RowTextActive: 0xFFFFFFFFu,
            HeadIdleText: 0xFFFFFFFFu,
            RowTextDim: 0x80B5B9C0u,
            SectionHeaderText: 0xFF96989Du,
            AheadColor: 0xFF9DD17Bu,
            BehindColor: 0xFFE6A85Cu),
        RepoBar: new RepoBarStyles(
            Background: 0xFF1E1F22u,
            RightBorder: 0xFF313338u),
        DiffView: new DiffViewStyles(
            PanelBackground: 0xFF1E1F22u,
            HeaderBackgroundIdle: 0xFF222326u,
            HeaderBackgroundHover: 0xFF3A3D43u,
            HeaderBorderTop: 0xFF313338u,
            HeaderBorderBottom: 0xFF313338u,
            HeaderTitleIdle: 0xFFB5B9C0u,
            HeaderTitleHover: 0xFFFFFFFFu),
        DiffContent: new DiffContentStyles(
            Background: 0xFF1E1F22u,
            PlaceholderText: 0xFF96989Du,
            ErrorText: 0xFFE9C77Au,
            LineText: 0xFFE6E6E6u,
            LineNumberText: 0xFF7A7C81u,
            LineAddedBackground: 0xFF284534u,
            LineAddedGlyph: 0xFF57F287u,
            LineRemovedBackground: 0xFF432528u,
            LineRemovedGlyph: 0xFFED4245u,
            LineContextGlyph: 0xFF96989Du,
            SectionBackground: 0xFF222326u,
            SectionMutedText: 0xFFB5B9C0u,
            HunkSeparatorRangeText: 0xFF96989Du,
            HunkOutline: 0xFF5A8DD6u),
        DiffHunkButton: new DiffHunkButtonStyles(
            BackgroundIdle: 0xCC2C313Au,
            BackgroundHover: 0xFF3B4150u,
            Border: 0xFF4A5060u,
            Text: 0xFFE6E8ECu),
        ActionsToolbar: new ActionsToolbarStyles(
            Background: 0xFF1E1F22u,
            BorderBottom: 0xFF313338u,
            BadgeAhead: 0xFF9DD17Bu,
            BadgeBehind: 0xFFE6A85Cu),
        SeparatorSpacer: new SeparatorSpacerStyles(
            Line: 0xFF313338u),
        SidebarSplitter: new SidebarSplitterStyles(
            Idle: 0xFF313338u,
            Hover: 0xFF4A5680u),
        HistorySplitter: new HistorySplitterStyles(
            HoverFill: 0xFF4A5680u,
            HoverLine: 0xFF7A8DC8u),
        ScrollBar: new ScrollBarStyles(
            TrackBackground: 0xFF26272Bu,
            TrackBorder: 0xFF313338u,
            ThumbIdleBackground: 0xFF4A4D52u,
            ThumbHoverBackground: 0xFF6A6D72u,
            ThumbBorder: 0xFF2A2C30u),
        LocalChangesView: new LocalChangesViewStyles(
            Background: 0xFF1E1F22u),
        Tooltip: new TooltipStyles(
            Background: 0xFF2A2C30u,
            Border: 0xFF313338u,
            Text: 0xFFE6E6E6u,
            Shadow: 0x80000000u),
        CommitsTruncationBar: new CommitsTruncationBarStyles(
            Background: 0xFF3D2E14u,
            BorderTop: 0xFFB89050u,
            Text: 0xFFE9C77Au),
        OperationBanner: new OperationBannerStyles(
            Background: 0xFF3D2E14u,
            BorderBottom: 0xFFB89050u,
            Text: 0xFFE9C77Au),
        OperationRow: new OperationRowStyles(
            IconText: 0xFFE6E6E6u,
            LabelText: 0xFFE6E6E6u,
            PhaseTextIdle: 0xFF7A7C81u,
            ElapsedText: 0xFF7A7C81u,
            BackgroundIdle: 0xFF2A2C30u,
            BackgroundHover: 0xFF313338u,
            SuccessBar: 0xFF4E8B3Du,
            SuccessText: 0xFF7FB76Au,
            FailureBar: 0xFFB3514Bu,
            FailureText: 0xFFE9C77Au));

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
            StatusOther: 0xFF7C3AEDu),
        DialogFrame: new DialogFrameStyles(
            Background: 0xFFFFFFFFu,
            Border: 0xFFE5E7EBu,
            TitleText: 0xFF111827u,
            HeaderSeparator: 0xFFE5E7EBu,
            ErrorText: 0xFFDC2626u),
        TextInput: new TextInputStyles(
            Background: 0xFFFFFFFFu,
            Border: 0xFFD1D5DBu,
            Text: 0xFF111827u,
            Caret: 0xFF111827u,
            Selection: 0xFFCBD5E1u,
            PlaceholderText: 0x806B7280u),
        BorderedButton: new BorderedButtonStyles(
            BackgroundIdle: 0xFFFFFFFFu,
            BackgroundHover: 0xFFF3F4F6u,
            BorderIdle: 0xFFD1D5DBu,
            BorderHover: 0xFF4F46E5u,
            Text: 0xFF111827u,
            TextDisabled: 0x80374151u),
        DialogIconButton: new DialogIconButtonStyles(
            BackgroundIdle: 0x00000000u,
            BackgroundHover: 0xFFE5E7EBu,
            TextIdle: 0xFF6B7280u,
            TextHover: 0xFF111827u),
        ActionButton: new ActionButtonStyles(
            BackgroundIdle: 0x00000000u,
            BackgroundHover: 0xFFE5E7EBu,
            TextIdle: 0xFF6B7280u,
            TextHover: 0xFF111827u,
            TextDisabled: 0x80374151u),
        Checkbox: new CheckboxStyles(
            TextIdle: 0xFF374151u,
            TextHover: 0xFF111827u,
            TextDisabled: 0x80374151u),
        CommitBar: new CommitBarStyles(
            Background: 0xFFF3F4F6u,
            TopBorder: 0xFFE5E7EBu),
        ErrorBar: new ErrorBarStyles(
            Background: 0xFFFEF3C7u,
            Border: 0xFFD97706u,
            Text: 0xFF78350Fu),
        ModeSwitcher: new ModeSwitcherStyles(
            PillBorder: 0xFFD1D5DBu,
            SegmentSeparator: 0xFFD1D5DBu,
            SegmentIdleBackground: 0x00000000u,
            SegmentHoverBackground: 0xFFF3F4F6u,
            SegmentActiveBackground: 0xFF4F46E5u,
            SegmentIdleText: 0xFF374151u,
            SegmentActiveText: 0xFFFFFFFFu),
        BranchesHeader: new BranchesHeaderStyles(
            Background: 0xFFFFFFFFu,
            BorderBottom: 0xFFE5E7EBu,
            PrefixText: 0xFF6B7280u,
            ActiveText: 0xFF111827u,
            DetachedText: 0x80374151u),
        AddRepoButton: new AddRepoButtonStyles(
            Text: 0xFF374151u),
        GroupHeaderRow: new GroupHeaderRowStyles(
            ChevronText: 0xFF6B7280u,
            BackgroundIdle: 0x00000000u,
            BackgroundHover: 0xFFF3F4F6u,
            NameText: 0xFF6B7280u),
        GroupRenameField: new GroupRenameFieldStyles(
            Background: 0xFFFFFFFFu,
            Border: 0xFF4F46E5u,
            Text: 0xFF111827u,
            Caret: 0xFF111827u,
            Selection: 0xFFCBD5E1u),
        WorktreeChevron: new WorktreeChevronStyles(
            Text: 0xFF374151u),
        RepoBarRow: new RepoBarRowStyles(
            BackgroundIdle: 0x00000000u,
            BackgroundHover: 0xFFF3F4F6u,
            BackgroundActive: 0xFF4F46E5u,
            TextIdle: 0xFF374151u,
            TextActive: 0xFFFFFFFFu,
            TextMissing: 0x80374151u,
            IconAccentWorktree: 0xFF0EA5E9u,
            IconAccentSubmodule: 0xFFA855F7u),
        BranchesView: new BranchesViewStyles(
            ViewBackground: 0xFFFFFFFFu,
            RowSelectedBackground: 0xFFE0E7FFu,
            RowHoverBackground: 0xFFF3F4F6u,
            RowText: 0xFF374151u,
            RowTextActive: 0xFF1E1B4Bu,
            HeadIdleText: 0xFF111827u,
            RowTextDim: 0x80374151u,
            SectionHeaderText: 0xFF6B7280u,
            AheadColor: 0xFF16A34Au,
            BehindColor: 0xFFEA580Cu),
        RepoBar: new RepoBarStyles(
            Background: 0xFFFFFFFFu,
            RightBorder: 0xFFE5E7EBu),
        DiffView: new DiffViewStyles(
            PanelBackground: 0xFFFFFFFFu,
            HeaderBackgroundIdle: 0xFFF3F4F6u,
            HeaderBackgroundHover: 0xFFE5E7EBu,
            HeaderBorderTop: 0xFFE5E7EBu,
            HeaderBorderBottom: 0xFFE5E7EBu,
            HeaderTitleIdle: 0xFF6B7280u,
            HeaderTitleHover: 0xFF111827u),
        DiffContent: new DiffContentStyles(
            Background: 0xFFFFFFFFu,
            PlaceholderText: 0xFF6B7280u,
            ErrorText: 0xFF92400Eu,
            LineText: 0xFF111827u,
            LineNumberText: 0xFF6B7280u,
            LineAddedBackground: 0xFFDCFCE7u,
            LineAddedGlyph: 0xFF15803Du,
            LineRemovedBackground: 0xFFFEE2E2u,
            LineRemovedGlyph: 0xFFB91C1Cu,
            LineContextGlyph: 0xFF6B7280u,
            SectionBackground: 0xFFF3F4F6u,
            SectionMutedText: 0xFF374151u,
            HunkSeparatorRangeText: 0xFF6B7280u,
            HunkOutline: 0xFF3B82F6u),
        DiffHunkButton: new DiffHunkButtonStyles(
            BackgroundIdle: 0xCCD1D5DBu,
            BackgroundHover: 0xFFE5E7EBu,
            Border: 0xFFD1D5DBu,
            Text: 0xFF111827u),
        ActionsToolbar: new ActionsToolbarStyles(
            Background: 0xFFFFFFFFu,
            BorderBottom: 0xFFE5E7EBu,
            BadgeAhead: 0xFF16A34Au,
            BadgeBehind: 0xFFEA580Cu),
        SeparatorSpacer: new SeparatorSpacerStyles(
            Line: 0xFFE5E7EBu),
        SidebarSplitter: new SidebarSplitterStyles(
            Idle: 0xFFE5E7EBu,
            Hover: 0xFFCBD5E1u),
        HistorySplitter: new HistorySplitterStyles(
            HoverFill: 0xFFCBD5E1u,
            HoverLine: 0xFF94A3B8u),
        ScrollBar: new ScrollBarStyles(
            TrackBackground: 0xFFF3F4F6u,
            TrackBorder: 0xFFE5E7EBu,
            ThumbIdleBackground: 0xFFC1C5CBu,
            ThumbHoverBackground: 0xFF9CA3AFu,
            ThumbBorder: 0xFFE5E7EBu),
        LocalChangesView: new LocalChangesViewStyles(
            Background: 0xFFFFFFFFu),
        Tooltip: new TooltipStyles(
            Background: 0xFF374151u,
            Border: 0xFF1F2937u,
            Text: 0xFFFFFFFFu,
            Shadow: 0x40000000u),
        CommitsTruncationBar: new CommitsTruncationBarStyles(
            Background: 0xFFFEF3C7u,
            BorderTop: 0xFFD97706u,
            Text: 0xFF78350Fu),
        OperationBanner: new OperationBannerStyles(
            Background: 0xFFFEF3C7u,
            BorderBottom: 0xFFD97706u,
            Text: 0xFF78350Fu),
        OperationRow: new OperationRowStyles(
            IconText: 0xFF111827u,
            LabelText: 0xFF111827u,
            PhaseTextIdle: 0xFF6B7280u,
            ElapsedText: 0xFF6B7280u,
            BackgroundIdle: 0xFFF3F4F6u,
            BackgroundHover: 0xFFE5E7EBu,
            SuccessBar: 0xFF16A34Au,
            SuccessText: 0xFF166534u,
            FailureBar: 0xFFDC2626u,
            FailureText: 0xFF7C2D12u));
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

public sealed record DialogFrameStyles(
    uint Background,
    uint Border,
    uint TitleText,
    uint HeaderSeparator,
    uint ErrorText);

public sealed record TextInputStyles(
    uint Background,
    uint Border,
    uint Text,
    uint Caret,
    uint Selection,
    uint PlaceholderText);

public sealed record BorderedButtonStyles(
    uint BackgroundIdle,
    uint BackgroundHover,
    uint BorderIdle,
    uint BorderHover,
    uint Text,
    uint TextDisabled);

public sealed record DialogIconButtonStyles(
    uint BackgroundIdle,
    uint BackgroundHover,
    uint TextIdle,
    uint TextHover);

public sealed record ActionButtonStyles(
    uint BackgroundIdle,
    uint BackgroundHover,
    uint TextIdle,
    uint TextHover,
    uint TextDisabled);

public sealed record CheckboxStyles(
    uint TextIdle,
    uint TextHover,
    uint TextDisabled);

public sealed record CommitBarStyles(
    uint Background,
    uint TopBorder);

public sealed record ErrorBarStyles(
    uint Background,
    uint Border,
    uint Text);

public sealed record ModeSwitcherStyles(
    uint PillBorder,
    uint SegmentSeparator,
    uint SegmentIdleBackground,
    uint SegmentHoverBackground,
    uint SegmentActiveBackground,
    uint SegmentIdleText,
    uint SegmentActiveText);

public sealed record BranchesHeaderStyles(
    uint Background,
    uint BorderBottom,
    uint PrefixText,
    uint ActiveText,
    uint DetachedText);

public sealed record AddRepoButtonStyles(
    uint Text);

public sealed record GroupHeaderRowStyles(
    uint ChevronText,
    uint BackgroundIdle,
    uint BackgroundHover,
    uint NameText);

public sealed record GroupRenameFieldStyles(
    uint Background,
    uint Border,
    uint Text,
    uint Caret,
    uint Selection);

public sealed record WorktreeChevronStyles(
    uint Text);

public sealed record RepoBarRowStyles(
    uint BackgroundIdle,
    uint BackgroundHover,
    uint BackgroundActive,
    uint TextIdle,
    uint TextActive,
    uint TextMissing,
    uint IconAccentWorktree,
    uint IconAccentSubmodule);

public sealed record BranchesViewStyles(
    uint ViewBackground,
    uint RowSelectedBackground,
    uint RowHoverBackground,
    uint RowText,
    uint RowTextActive,
    uint HeadIdleText,
    uint RowTextDim,
    uint SectionHeaderText,
    uint AheadColor,
    uint BehindColor);

public sealed record RepoBarStyles(
    uint Background,
    uint RightBorder);

public sealed record DiffViewStyles(
    uint PanelBackground,
    uint HeaderBackgroundIdle,
    uint HeaderBackgroundHover,
    uint HeaderBorderTop,
    uint HeaderBorderBottom,
    uint HeaderTitleIdle,
    uint HeaderTitleHover);

public sealed record DiffContentStyles(
    uint Background,
    uint PlaceholderText,
    uint ErrorText,
    uint LineText,
    uint LineNumberText,
    uint LineAddedBackground,
    uint LineAddedGlyph,
    uint LineRemovedBackground,
    uint LineRemovedGlyph,
    uint LineContextGlyph,
    uint SectionBackground,
    uint SectionMutedText,
    uint HunkSeparatorRangeText,
    uint HunkOutline);

public sealed record DiffHunkButtonStyles(
    uint BackgroundIdle,
    uint BackgroundHover,
    uint Border,
    uint Text);

public sealed record ActionsToolbarStyles(
    uint Background,
    uint BorderBottom,
    uint BadgeAhead,
    uint BadgeBehind);

public sealed record SeparatorSpacerStyles(
    uint Line);

public sealed record SidebarSplitterStyles(
    uint Idle,
    uint Hover);

public sealed record HistorySplitterStyles(
    uint HoverFill,
    uint HoverLine);

public sealed record ScrollBarStyles(
    uint TrackBackground,
    uint TrackBorder,
    uint ThumbIdleBackground,
    uint ThumbHoverBackground,
    uint ThumbBorder);

public sealed record LocalChangesViewStyles(
    uint Background);

public sealed record TooltipStyles(
    uint Background,
    uint Border,
    uint Text,
    uint Shadow);

public sealed record CommitsTruncationBarStyles(
    uint Background,
    uint BorderTop,
    uint Text);

public sealed record OperationBannerStyles(
    uint Background,
    uint BorderBottom,
    uint Text);

public sealed record OperationRowStyles(
    uint IconText,
    uint LabelText,
    uint PhaseTextIdle,
    uint ElapsedText,
    uint BackgroundIdle,
    uint BackgroundHover,
    uint SuccessBar,
    uint SuccessText,
    uint FailureBar,
    uint FailureText);

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
