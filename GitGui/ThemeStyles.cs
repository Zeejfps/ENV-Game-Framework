namespace GitGui;

public sealed record ThemeStyles(
    ThemePalette Palette,
    StatusPalette Status,
    BannerStyles Banner,
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
    ModeSwitcherStyles ModeSwitcher,
    BranchesHeaderStyles BranchesHeader,
    GroupHeaderRowStyles GroupHeaderRow,
    GroupRenameFieldStyles GroupRenameField,
    RepoBarRowStyles RepoBarRow,
    BranchesViewStyles BranchesView,
    RepoBarStyles RepoBar,
    DiffViewStyles DiffView,
    DiffContentStyles DiffContent,
    DiffHunkButtonStyles DiffHunkButton,
    ActionsToolbarStyles ActionsToolbar,
    SidebarSplitterStyles SidebarSplitter,
    HistorySplitterStyles HistorySplitter,
    ScrollBarStyles ScrollBar,
    TooltipStyles Tooltip,
    OperationRowStyles OperationRow,
    CommitDetailsViewStyles CommitDetailsView,
    DialogBodyStyles DialogBody,
    BranchPreviewStyles BranchPreview,
    ContextMenuStyles ContextMenu,
    OperationsStatusBarStyles OperationsStatusBar,
    CommitsViewStyles CommitsView)
{
    public static readonly ThemeStyles Dark = BuildDark();
    public static readonly ThemeStyles Light = BuildLight();

    private static ThemeStyles BuildDark()
    {
        var p = new ThemePalette(
            Surface: 0xFF1E1F22u,
            SurfaceRaised: 0xFF222326u,
            SurfaceSunken: 0xFF1A1B1Eu,
            SurfaceMuted: 0xFF2A2C30u,
            SurfaceHover: 0xFF2B2D31u,
            SurfaceHoverStrong: 0xFF3A3D43u,
            SurfaceSelected: 0xFF404C8Cu,
            SurfaceSelectedSubtle: 0xFF404C8Cu,
            Border: 0xFF313338u,
            BorderStrong: 0xFF3E4047u,
            BorderMuted: 0xFF4A4D52u,
            BorderMutedHover: 0xFF6A6D72u,
            BorderHoverFill: 0xFF4A5680u,
            BorderHoverLine: 0xFF7A8DC8u,
            Accent: 0xFF5865F2u,
            AccentHover: 0xFF7480F5u,
            TextStrong: 0xFFFFFFFFu,
            TextPrimary: 0xFFE6E6E6u,
            TextSecondary: 0xFFB5B9C0u,
            TextMuted: 0xFF96989Du,
            TextDim: 0xFF7A7C81u,
            TextDisabled: 0x80B5B9C0u,
            TextOnAccent: 0xFFFFFFFFu,
            Shadow: 0x80000000u);

        var status = new StatusPalette(
            Success: 0xFF57F287u,
            Warning: 0xFFE9C77Au,
            Danger: 0xFFED4245u,
            Info: 0xFF5DADE2u,
            Purple: 0xFFB57EDCu,
            SuccessSoft: 0xFF9DD17Bu,
            WarningSoft: 0xFFE6A85Cu);

        var banner = new BannerStyles(
            Background: 0xFF3D2E14u,
            Border: 0xFFB89050u,
            Text: 0xFFE9C77Au);

        return new ThemeStyles(
            Palette: p,
            Status: status,
            Banner: banner,
            HeaderActionButton: new HeaderActionButtonStyles(
                Background: 0u,
                BackgroundHover: p.SurfaceHoverStrong,
                IconIdle: p.TextSecondary,
                IconHover: p.TextStrong,
                IconDisabled: 0x66B5B9C0u),
            LocalChangesContent: new LocalChangesContentStyles(
                ContentBackground: p.Surface,
                ColumnDivider: p.Border,
                PlaceholderText: p.TextMuted,
                SplitterIdle: p.Border,
                SplitterHover: p.BorderHoverFill),
            SubmoduleSection: new SubmoduleSectionStyles(
                BadgeBackground: status.Purple,
                BadgeText: p.SurfaceSunken,
                RowText: p.TextSecondary),
            FileChangesSection: new FileChangesSectionStyles(
                HeaderBackground: p.SurfaceRaised,
                HeaderBorder: p.Border,
                HeaderText: p.TextMuted,
                EmptyPlaceholderText: p.TextMuted),
            FileChangeRow: new FileChangeRowStyles(
                RowText: p.TextSecondary,
                RowTextActive: p.TextOnAccent,
                RowHover: p.SurfaceHover,
                RowActive: p.SurfaceSelected,
                BadgeText: p.SurfaceSunken,
                StatusAdded: status.Success,
                StatusModified: status.Warning,
                StatusDeleted: status.Danger,
                StatusRenamed: status.Info,
                StatusConflicted: status.Danger,
                StatusSubmodule: status.Purple,
                StatusOther: 0xFF9B59B6u),
            DialogFrame: new DialogFrameStyles(
                Background: p.Surface,
                Border: p.Border,
                TitleText: p.TextPrimary,
                HeaderSeparator: p.SurfaceMuted,
                ErrorText: 0xFFE06C75u,
                InsetBackground: p.SurfaceSunken),
            TextInput: new TextInputStyles(
                Background: p.SurfaceHover,
                Border: p.BorderStrong,
                Text: p.TextPrimary,
                Caret: p.TextPrimary,
                Selection: p.SurfaceSelected,
                PlaceholderText: p.TextDisabled),
            BorderedButton: new BorderedButtonStyles(
                BackgroundIdle: p.SurfaceHover,
                BackgroundHover: p.SurfaceHoverStrong,
                BorderIdle: p.BorderStrong,
                BorderHover: p.Accent,
                Text: p.TextStrong,
                TextDisabled: p.TextDisabled),
            DialogIconButton: new DialogIconButtonStyles(
                BackgroundIdle: 0u,
                BackgroundHover: p.SurfaceHoverStrong,
                TextIdle: p.TextSecondary,
                TextHover: p.TextStrong),
            ActionButton: new ActionButtonStyles(
                BackgroundIdle: 0u,
                BackgroundHover: p.SurfaceHoverStrong,
                TextIdle: p.TextSecondary,
                TextHover: p.TextStrong,
                TextDisabled: p.TextDisabled),
            Checkbox: new CheckboxStyles(
                TextIdle: p.TextSecondary,
                TextHover: p.TextStrong,
                TextDisabled: p.TextDisabled,
                BoxBorderIdle: p.BorderMuted,
                BoxBorderHover: p.BorderMutedHover,
                BoxBorderDisabled: 0x664A4D52u,
                BoxFillChecked: p.Accent,
                BoxFillCheckedHover: p.AccentHover,
                BoxFillDisabled: p.SurfaceHover,
                CheckGlyph: p.TextStrong),
            CommitBar: new CommitBarStyles(
                Background: p.SurfaceMuted,
                TopBorder: p.Border),
            ModeSwitcher: new ModeSwitcherStyles(
                PillBorder: p.BorderStrong,
                SegmentSeparator: p.BorderStrong,
                SegmentIdleBackground: 0u,
                SegmentHoverBackground: p.SurfaceHoverStrong,
                SegmentActiveBackground: p.SurfaceSelected,
                SegmentIdleText: p.TextSecondary,
                SegmentHoverText: p.TextStrong,
                SegmentActiveText: p.TextOnAccent),
            BranchesHeader: new BranchesHeaderStyles(
                Background: p.Surface,
                BorderBottom: p.Border,
                PrefixText: p.TextMuted,
                ActiveText: p.TextStrong,
                DetachedText: p.TextDisabled),
            GroupHeaderRow: new GroupHeaderRowStyles(
                ChevronText: p.TextMuted,
                BackgroundIdle: 0u,
                BackgroundHover: p.SurfaceHover,
                NameText: p.TextMuted),
            GroupRenameField: new GroupRenameFieldStyles(
                Background: p.SurfaceHover,
                Border: p.Accent,
                Text: p.TextPrimary,
                Caret: p.TextPrimary,
                Selection: p.SurfaceSelected),
            RepoBarRow: new RepoBarRowStyles(
                BackgroundIdle: 0u,
                BackgroundHover: p.SurfaceHover,
                BackgroundActive: p.SurfaceSelected,
                TextIdle: p.TextSecondary,
                TextActive: p.TextOnAccent,
                TextMissing: p.TextDisabled,
                IconAccentWorktree: status.Info,
                IconAccentSubmodule: status.Purple),
            BranchesView: new BranchesViewStyles(
                ViewBackground: p.Surface,
                RowSelectedBackground: p.SurfaceSelectedSubtle,
                RowHoverBackground: p.SurfaceHover,
                RowText: p.TextSecondary,
                RowTextActive: p.TextOnAccent,
                HeadIdleText: p.TextStrong,
                RowTextDim: p.TextDisabled,
                SectionHeaderText: p.TextMuted,
                AheadColor: status.SuccessSoft,
                BehindColor: status.WarningSoft),
            RepoBar: new RepoBarStyles(
                Background: p.Surface,
                RightBorder: p.Border),
            DiffView: new DiffViewStyles(
                PanelBackground: p.Surface,
                HeaderBackgroundIdle: p.SurfaceRaised,
                HeaderBackgroundHover: p.SurfaceHoverStrong,
                HeaderBorderTop: p.Border,
                HeaderBorderBottom: p.Border,
                HeaderTitleIdle: p.TextSecondary,
                HeaderTitleHover: p.TextStrong),
            DiffContent: new DiffContentStyles(
                Background: p.Surface,
                PlaceholderText: p.TextMuted,
                ErrorText: 0xFFE9C77Au,
                LineText: p.TextPrimary,
                LineNumberText: p.TextDim,
                LineAddedBackground: 0xFF284534u,
                LineAddedGlyph: status.Success,
                LineRemovedBackground: 0xFF432528u,
                LineRemovedGlyph: status.Danger,
                LineContextGlyph: p.TextMuted,
                SectionBackground: p.SurfaceRaised,
                SectionMutedText: p.TextSecondary,
                HunkSeparatorRangeText: p.TextMuted,
                HunkOutline: 0xFF5A8DD6u),
            DiffHunkButton: new DiffHunkButtonStyles(
                BackgroundIdle: 0xCC2C313Au,
                BackgroundHover: 0xFF3B4150u,
                Border: 0xFF4A5060u,
                Text: 0xFFE6E8ECu),
            ActionsToolbar: new ActionsToolbarStyles(
                Background: p.Surface,
                BorderBottom: p.Border,
                BadgeAhead: status.SuccessSoft,
                BadgeBehind: status.WarningSoft),
            SidebarSplitter: new SidebarSplitterStyles(
                Idle: p.Border,
                Hover: p.BorderHoverFill),
            HistorySplitter: new HistorySplitterStyles(
                HoverFill: p.BorderHoverFill,
                HoverLine: p.BorderHoverLine),
            ScrollBar: new ScrollBarStyles(
                TrackBackground: 0xFF26272Bu,
                TrackBorder: p.Border,
                ThumbIdleBackground: p.BorderMuted,
                ThumbHoverBackground: p.BorderMutedHover,
                ThumbBorder: p.SurfaceMuted),
            Tooltip: new TooltipStyles(
                Background: p.SurfaceMuted,
                Border: p.Border,
                Text: p.TextPrimary,
                Shadow: p.Shadow),
            OperationRow: new OperationRowStyles(
                IconText: p.TextPrimary,
                LabelText: p.TextPrimary,
                PhaseTextIdle: p.TextDim,
                ElapsedText: p.TextDim,
                BackgroundIdle: p.SurfaceMuted,
                BackgroundHover: p.Border,
                SuccessBar: 0xFF4E8B3Du,
                SuccessText: 0xFF7FB76Au,
                FailureBar: 0xFFB3514Bu,
                FailureText: status.Warning),
            CommitDetailsView: new CommitDetailsViewStyles(
                Background: p.SurfaceSunken,
                BorderLeft: p.Border,
                PrimaryText: p.TextPrimary,
                SecondaryText: p.TextSecondary,
                MutedText: p.TextDim,
                PlaceholderText: p.TextMuted,
                SplitterIdle: p.Border,
                SplitterHover: p.BorderHoverFill),
            DialogBody: new DialogBodyStyles(
                BodyText: 0xFFDCDDDEu,
                SectionHeaderText: p.TextMuted,
                RowText: p.TextSecondary,
                RowTextMissing: p.TextDisabled),
            BranchPreview: new BranchPreviewStyles(
                Clean: status.SuccessSoft,
                Conflict: status.WarningSoft),
            ContextMenu: new ContextMenuStyles(
                Background: p.Surface,
                Border: p.Border,
                ItemSelectedBackground: p.SurfaceHover,
                ItemText: p.TextSecondary,
                ItemTextDisabled: p.TextDisabled,
                AccentText: p.TextStrong),
            OperationsStatusBar: new OperationsStatusBarStyles(
                ContainerBackground: p.SurfaceMuted,
                ContainerBorder: p.Border,
                LogBackground: p.SurfaceSunken,
                LogBorder: p.Border,
                LogText: p.TextSecondary),
            CommitsView: new CommitsViewStyles(
                Background: p.Surface,
                HeaderBackground: p.SurfaceMuted,
                HeaderBorderBottom: p.Border,
                HeaderText: p.TextMuted,
                RowText: p.TextSecondary,
                RowTextActive: p.TextOnAccent,
                RowTextDim: p.TextDim,
                RowSelectedBackground: p.SurfaceSelected,
                PlaceholderText: p.TextMuted,
                ColumnDividerIdle: p.Border,
                ColumnDividerHoverFill: p.BorderHoverFill,
                ColumnDividerHoverLine: p.BorderHoverLine,
                BadgeLocalBackground: 0xFF2F4A6Bu,
                BadgeRemoteBackground: 0xFF4A2F6Bu,
                BadgeHeadBackground: 0xFF6B4A2Fu,
                BadgeText: p.TextPrimary));
    }

    private static ThemeStyles BuildLight()
    {
        var p = new ThemePalette(
            Surface: 0xFFFFFFFFu,
            SurfaceRaised: 0xFFF3F4F6u,
            SurfaceSunken: 0xFFF9FAFBu,
            SurfaceMuted: 0xFFF3F4F6u,
            SurfaceHover: 0xFFF3F4F6u,
            SurfaceHoverStrong: 0xFFE5E7EBu,
            SurfaceSelected: 0xFF4F46E5u,
            SurfaceSelectedSubtle: 0xFFE0E7FFu,
            Border: 0xFFE5E7EBu,
            BorderStrong: 0xFFD1D5DBu,
            BorderMuted: 0xFFC1C5CBu,
            BorderMutedHover: 0xFF9CA3AFu,
            BorderHoverFill: 0xFFCBD5E1u,
            BorderHoverLine: 0xFF94A3B8u,
            Accent: 0xFF4F46E5u,
            AccentHover: 0xFF6366F1u,
            TextStrong: 0xFF111827u,
            TextPrimary: 0xFF111827u,
            TextSecondary: 0xFF374151u,
            TextMuted: 0xFF6B7280u,
            TextDim: 0xFF9CA3AFu,
            TextDisabled: 0x80374151u,
            TextOnAccent: 0xFFFFFFFFu,
            Shadow: 0x40000000u);

        var status = new StatusPalette(
            Success: 0xFF16A34Au,
            Warning: 0xFFCA8A04u,
            Danger: 0xFFDC2626u,
            Info: 0xFF2563EBu,
            Purple: 0xFFA855F7u,
            SuccessSoft: 0xFF16A34Au,
            WarningSoft: 0xFFEA580Cu);

        var banner = new BannerStyles(
            Background: 0xFFFEF3C7u,
            Border: 0xFFD97706u,
            Text: 0xFF78350Fu);

        return new ThemeStyles(
            Palette: p,
            Status: status,
            Banner: banner,
            HeaderActionButton: new HeaderActionButtonStyles(
                Background: 0u,
                BackgroundHover: p.SurfaceHoverStrong,
                IconIdle: 0xFF4B5563u,
                IconHover: p.TextStrong,
                IconDisabled: 0x664B5563u),
            LocalChangesContent: new LocalChangesContentStyles(
                ContentBackground: p.Surface,
                ColumnDivider: p.Border,
                PlaceholderText: p.TextMuted,
                SplitterIdle: p.Border,
                SplitterHover: p.BorderHoverFill),
            SubmoduleSection: new SubmoduleSectionStyles(
                BadgeBackground: status.Purple,
                BadgeText: p.TextOnAccent,
                RowText: 0xFF4B5563u),
            FileChangesSection: new FileChangesSectionStyles(
                HeaderBackground: p.SurfaceRaised,
                HeaderBorder: p.Border,
                HeaderText: p.TextMuted,
                EmptyPlaceholderText: p.TextMuted),
            FileChangeRow: new FileChangeRowStyles(
                RowText: p.TextSecondary,
                RowTextActive: p.TextOnAccent,
                RowHover: p.SurfaceHover,
                RowActive: p.SurfaceSelected,
                BadgeText: p.TextOnAccent,
                StatusAdded: status.Success,
                StatusModified: status.Warning,
                StatusDeleted: status.Danger,
                StatusRenamed: status.Info,
                StatusConflicted: status.Danger,
                StatusSubmodule: status.Purple,
                StatusOther: 0xFF7C3AEDu),
            DialogFrame: new DialogFrameStyles(
                Background: p.Surface,
                Border: p.Border,
                TitleText: p.TextStrong,
                HeaderSeparator: p.Border,
                ErrorText: status.Danger,
                InsetBackground: p.SurfaceSunken),
            TextInput: new TextInputStyles(
                Background: p.Surface,
                Border: p.BorderStrong,
                Text: p.TextStrong,
                Caret: p.TextStrong,
                Selection: p.BorderHoverFill,
                PlaceholderText: 0x806B7280u),
            BorderedButton: new BorderedButtonStyles(
                BackgroundIdle: p.Surface,
                BackgroundHover: p.SurfaceHover,
                BorderIdle: p.BorderStrong,
                BorderHover: p.Accent,
                Text: p.TextStrong,
                TextDisabled: p.TextDisabled),
            DialogIconButton: new DialogIconButtonStyles(
                BackgroundIdle: 0u,
                BackgroundHover: p.SurfaceHoverStrong,
                TextIdle: p.TextMuted,
                TextHover: p.TextStrong),
            ActionButton: new ActionButtonStyles(
                BackgroundIdle: 0u,
                BackgroundHover: p.SurfaceHoverStrong,
                TextIdle: p.TextMuted,
                TextHover: p.TextStrong,
                TextDisabled: p.TextDisabled),
            Checkbox: new CheckboxStyles(
                TextIdle: p.TextSecondary,
                TextHover: p.TextStrong,
                TextDisabled: p.TextDisabled,
                BoxBorderIdle: p.BorderStrong,
                BoxBorderHover: p.BorderMutedHover,
                BoxBorderDisabled: 0x66D1D5DBu,
                BoxFillChecked: p.Accent,
                BoxFillCheckedHover: p.AccentHover,
                BoxFillDisabled: p.Border,
                CheckGlyph: p.TextOnAccent),
            CommitBar: new CommitBarStyles(
                Background: p.SurfaceRaised,
                TopBorder: p.Border),
            ModeSwitcher: new ModeSwitcherStyles(
                PillBorder: p.BorderStrong,
                SegmentSeparator: p.BorderStrong,
                SegmentIdleBackground: 0u,
                SegmentHoverBackground: p.SurfaceHover,
                SegmentActiveBackground: p.Accent,
                SegmentIdleText: p.TextSecondary,
                SegmentHoverText: p.TextStrong,
                SegmentActiveText: p.TextOnAccent),
            BranchesHeader: new BranchesHeaderStyles(
                Background: p.Surface,
                BorderBottom: p.Border,
                PrefixText: p.TextMuted,
                ActiveText: p.TextStrong,
                DetachedText: p.TextDisabled),
            GroupHeaderRow: new GroupHeaderRowStyles(
                ChevronText: p.TextMuted,
                BackgroundIdle: 0u,
                BackgroundHover: p.SurfaceHover,
                NameText: p.TextMuted),
            GroupRenameField: new GroupRenameFieldStyles(
                Background: p.Surface,
                Border: p.Accent,
                Text: p.TextStrong,
                Caret: p.TextStrong,
                Selection: p.BorderHoverFill),
            RepoBarRow: new RepoBarRowStyles(
                BackgroundIdle: 0u,
                BackgroundHover: p.SurfaceHover,
                BackgroundActive: p.Accent,
                TextIdle: p.TextSecondary,
                TextActive: p.TextOnAccent,
                TextMissing: p.TextDisabled,
                IconAccentWorktree: 0xFF0EA5E9u,
                IconAccentSubmodule: status.Purple),
            BranchesView: new BranchesViewStyles(
                ViewBackground: p.Surface,
                RowSelectedBackground: p.SurfaceSelectedSubtle,
                RowHoverBackground: p.SurfaceHover,
                RowText: p.TextSecondary,
                RowTextActive: 0xFF1E1B4Bu,
                HeadIdleText: p.TextStrong,
                RowTextDim: p.TextDisabled,
                SectionHeaderText: p.TextMuted,
                AheadColor: status.SuccessSoft,
                BehindColor: status.WarningSoft),
            RepoBar: new RepoBarStyles(
                Background: p.Surface,
                RightBorder: p.Border),
            DiffView: new DiffViewStyles(
                PanelBackground: p.Surface,
                HeaderBackgroundIdle: p.SurfaceRaised,
                HeaderBackgroundHover: p.SurfaceHoverStrong,
                HeaderBorderTop: p.Border,
                HeaderBorderBottom: p.Border,
                HeaderTitleIdle: p.TextMuted,
                HeaderTitleHover: p.TextStrong),
            DiffContent: new DiffContentStyles(
                Background: p.Surface,
                PlaceholderText: p.TextMuted,
                ErrorText: 0xFF92400Eu,
                LineText: p.TextStrong,
                LineNumberText: p.TextMuted,
                LineAddedBackground: 0xFFDCFCE7u,
                LineAddedGlyph: 0xFF15803Du,
                LineRemovedBackground: 0xFFFEE2E2u,
                LineRemovedGlyph: 0xFFB91C1Cu,
                LineContextGlyph: p.TextMuted,
                SectionBackground: p.SurfaceRaised,
                SectionMutedText: p.TextSecondary,
                HunkSeparatorRangeText: p.TextMuted,
                HunkOutline: 0xFF3B82F6u),
            DiffHunkButton: new DiffHunkButtonStyles(
                BackgroundIdle: 0xCCD1D5DBu,
                BackgroundHover: p.SurfaceHoverStrong,
                Border: p.BorderStrong,
                Text: p.TextStrong),
            ActionsToolbar: new ActionsToolbarStyles(
                Background: p.Surface,
                BorderBottom: p.Border,
                BadgeAhead: status.SuccessSoft,
                BadgeBehind: status.WarningSoft),
            SidebarSplitter: new SidebarSplitterStyles(
                Idle: p.Border,
                Hover: p.BorderHoverFill),
            HistorySplitter: new HistorySplitterStyles(
                HoverFill: p.BorderHoverFill,
                HoverLine: p.BorderHoverLine),
            ScrollBar: new ScrollBarStyles(
                TrackBackground: p.SurfaceRaised,
                TrackBorder: p.Border,
                ThumbIdleBackground: p.BorderMuted,
                ThumbHoverBackground: p.BorderMutedHover,
                ThumbBorder: p.Border),
            Tooltip: new TooltipStyles(
                Background: 0xFF374151u,
                Border: 0xFF1F2937u,
                Text: p.TextOnAccent,
                Shadow: p.Shadow),
            OperationRow: new OperationRowStyles(
                IconText: p.TextStrong,
                LabelText: p.TextStrong,
                PhaseTextIdle: p.TextMuted,
                ElapsedText: p.TextMuted,
                BackgroundIdle: p.SurfaceRaised,
                BackgroundHover: p.SurfaceHoverStrong,
                SuccessBar: status.Success,
                SuccessText: 0xFF166534u,
                FailureBar: status.Danger,
                FailureText: 0xFF7C2D12u),
            CommitDetailsView: new CommitDetailsViewStyles(
                Background: p.SurfaceSunken,
                BorderLeft: p.Border,
                PrimaryText: p.TextStrong,
                SecondaryText: p.TextSecondary,
                MutedText: p.TextMuted,
                PlaceholderText: p.TextDim,
                SplitterIdle: p.Border,
                SplitterHover: p.BorderHoverFill),
            DialogBody: new DialogBodyStyles(
                BodyText: 0xFF1F2937u,
                SectionHeaderText: p.TextMuted,
                RowText: p.TextSecondary,
                RowTextMissing: p.TextDisabled),
            BranchPreview: new BranchPreviewStyles(
                Clean: status.SuccessSoft,
                Conflict: status.WarningSoft),
            ContextMenu: new ContextMenuStyles(
                Background: p.Surface,
                Border: p.Border,
                ItemSelectedBackground: p.SurfaceHover,
                ItemText: p.TextSecondary,
                ItemTextDisabled: p.TextDisabled,
                AccentText: p.TextStrong),
            OperationsStatusBar: new OperationsStatusBarStyles(
                ContainerBackground: p.SurfaceRaised,
                ContainerBorder: p.Border,
                LogBackground: p.SurfaceSunken,
                LogBorder: p.Border,
                LogText: p.TextSecondary),
            CommitsView: new CommitsViewStyles(
                Background: p.Surface,
                HeaderBackground: p.SurfaceRaised,
                HeaderBorderBottom: p.Border,
                HeaderText: p.TextMuted,
                RowText: p.TextSecondary,
                RowTextActive: p.TextStrong,
                RowTextDim: p.TextDim,
                RowSelectedBackground: p.SurfaceSelectedSubtle,
                PlaceholderText: p.TextDim,
                ColumnDividerIdle: p.Border,
                ColumnDividerHoverFill: p.BorderHoverFill,
                ColumnDividerHoverLine: p.BorderHoverLine,
                BadgeLocalBackground: 0xFFDBEAFEu,
                BadgeRemoteBackground: 0xFFEDE9FEu,
                BadgeHeadBackground: 0xFFFEF3C7u,
                BadgeText: 0xFF1F2937u));
    }
}

public sealed record ThemePalette(
    uint Surface,
    uint SurfaceRaised,
    uint SurfaceSunken,
    uint SurfaceMuted,
    uint SurfaceHover,
    uint SurfaceHoverStrong,
    uint SurfaceSelected,
    uint SurfaceSelectedSubtle,
    uint Border,
    uint BorderStrong,
    uint BorderMuted,
    uint BorderMutedHover,
    uint BorderHoverFill,
    uint BorderHoverLine,
    uint Accent,
    uint AccentHover,
    uint TextStrong,
    uint TextPrimary,
    uint TextSecondary,
    uint TextMuted,
    uint TextDim,
    uint TextDisabled,
    uint TextOnAccent,
    uint Shadow);

public sealed record StatusPalette(
    uint Success,
    uint Warning,
    uint Danger,
    uint Info,
    uint Purple,
    uint SuccessSoft,
    uint WarningSoft);

public sealed record BannerStyles(
    uint Background,
    uint Border,
    uint Text);

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
    uint ErrorText,
    uint InsetBackground);

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
    uint TextDisabled,
    uint BoxBorderIdle,
    uint BoxBorderHover,
    uint BoxBorderDisabled,
    uint BoxFillChecked,
    uint BoxFillCheckedHover,
    uint BoxFillDisabled,
    uint CheckGlyph);

public sealed record CommitBarStyles(
    uint Background,
    uint TopBorder);

public sealed record ModeSwitcherStyles(
    uint PillBorder,
    uint SegmentSeparator,
    uint SegmentIdleBackground,
    uint SegmentHoverBackground,
    uint SegmentActiveBackground,
    uint SegmentIdleText,
    uint SegmentHoverText,
    uint SegmentActiveText);

public sealed record BranchesHeaderStyles(
    uint Background,
    uint BorderBottom,
    uint PrefixText,
    uint ActiveText,
    uint DetachedText);

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

public sealed record TooltipStyles(
    uint Background,
    uint Border,
    uint Text,
    uint Shadow);

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

public sealed record CommitDetailsViewStyles(
    uint Background,
    uint BorderLeft,
    uint PrimaryText,
    uint SecondaryText,
    uint MutedText,
    uint PlaceholderText,
    uint SplitterIdle,
    uint SplitterHover);

public sealed record DialogBodyStyles(
    uint BodyText,
    uint SectionHeaderText,
    uint RowText,
    uint RowTextMissing);

public sealed record BranchPreviewStyles(
    uint Clean,
    uint Conflict);

public sealed record ContextMenuStyles(
    uint Background,
    uint Border,
    uint ItemSelectedBackground,
    uint ItemText,
    uint ItemTextDisabled,
    uint AccentText);

public sealed record OperationsStatusBarStyles(
    uint ContainerBackground,
    uint ContainerBorder,
    uint LogBackground,
    uint LogBorder,
    uint LogText);

public sealed record CommitsViewStyles(
    uint Background,
    uint HeaderBackground,
    uint HeaderBorderBottom,
    uint HeaderText,
    uint RowText,
    uint RowTextActive,
    uint RowTextDim,
    uint RowSelectedBackground,
    uint PlaceholderText,
    uint ColumnDividerIdle,
    uint ColumnDividerHoverFill,
    uint ColumnDividerHoverLine,
    uint BadgeLocalBackground,
    uint BadgeRemoteBackground,
    uint BadgeHeadBackground,
    uint BadgeText);

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
