namespace GitGui;

public sealed record ThemeStyles(
    ThemePalette Palette,
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

    private static uint WithAlpha(uint color, byte alpha) =>
        (color & 0x00FFFFFFu) | ((uint)alpha << 24);

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
            TextBody: 0xFFDCDDDEu,
            TextSecondary: 0xFFB5B9C0u,
            TextMedium: 0xFFB5B9C0u,
            TextMuted: 0xFF96989Du,
            TextDim: 0xFF7A7C81u,
            TextDisabled: 0x80B5B9C0u,
            TextOnAccent: 0xFFFFFFFFu,
            Shadow: 0x80000000u,
            BarSurface: 0xFF2A2C30u,
            InputSurface: 0xFF2B2D31u,
            InputSurfaceHover: 0xFF3A3D43u,
            TextEmphasis: 0xFFE6E6E6u,
            TextSubtle: 0xFFB5B9C0u,
            TextFaint: 0xFF7A7C81u,
            OnStatusText: 0xFF1A1B1Eu,
            RowSubtleText: 0xFFFFFFFFu,
            HunkOutline: 0xFF5A8DD6u,
            Selection: 0xFF404C8Cu,
            Placeholder: 0x80B5B9C0u,
            DialogHeaderSeparator: 0xFF2A2C30u,
            CheckboxBorderIdle: 0xFF4A4D52u,
            CheckboxDisabledFill: 0xFF2B2D31u,
            SegmentActiveBg: 0xFF404C8Cu,
            ScrollBarTrackBg: 0xFF26272Bu,
            ScrollBarThumbBorder: 0xFF2A2C30u,
            OperationRowHoverBg: 0xFF313338u,
            CommitRowSelectedBg: 0xFF404C8Cu,
            CommitRowSelectedText: 0xFFFFFFFFu);

        var status = new StatusPalette(
            Success: 0xFF57F287u,
            Warning: 0xFFE9C77Au,
            Danger: 0xFFED4245u,
            Info: 0xFF5DADE2u,
            Purple: 0xFFB57EDCu,
            SuccessSoft: 0xFF9DD17Bu,
            WarningSoft: 0xFFE6A85Cu,
            SuccessBar: 0xFF4E8B3Du,
            SuccessText: 0xFF7FB76Au,
            SuccessLineBg: 0xFF284534u,
            SuccessLineGlyph: 0xFF57F287u,
            DangerBar: 0xFFB3514Bu,
            DangerText: 0xFFE9C77Au,
            DangerLineBg: 0xFF432528u,
            DangerLineGlyph: 0xFFED4245u,
            Other: 0xFF9B59B6u,
            DialogError: 0xFFE06C75u,
            DiffError: 0xFFE9C77Au);

        var banner = new BannerStyles(
            Background: 0xFF3D2E14u,
            Border: 0xFFB89050u,
            Text: 0xFFE9C77Au);

        var tooltip = new TooltipPalette(
            Background: p.SurfaceMuted,
            Border: p.Border,
            Text: p.TextPrimary);

        var hunkButton = new DiffHunkButtonPalette(
            BackgroundIdle: 0xCC2C313Au,
            BackgroundHover: 0xFF3B4150u,
            Border: 0xFF4A5060u,
            Text: 0xFFE6E8ECu);

        var commitBadge = new CommitBadgePalette(
            LocalBg: 0xFF2F4A6Bu,
            RemoteBg: 0xFF4A2F6Bu,
            HeadBg: 0xFF6B4A2Fu,
            Text: p.TextPrimary);

        return BuildStyles(p, status, banner, tooltip, hunkButton, commitBadge);
    }

    private static ThemeStyles BuildLight()
    {
        var textMutedLight = 0xFF6B7280u;

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
            TextBody: 0xFF1F2937u,
            TextSecondary: 0xFF374151u,
            TextMedium: 0xFF4B5563u,
            TextMuted: textMutedLight,
            TextDim: 0xFF9CA3AFu,
            TextDisabled: 0x80374151u,
            TextOnAccent: 0xFFFFFFFFu,
            Shadow: 0x40000000u,
            BarSurface: 0xFFF3F4F6u,
            InputSurface: 0xFFFFFFFFu,
            InputSurfaceHover: 0xFFF3F4F6u,
            TextEmphasis: 0xFF111827u,
            TextSubtle: textMutedLight,
            TextFaint: textMutedLight,
            OnStatusText: 0xFFFFFFFFu,
            RowSubtleText: 0xFF1E1B4Bu,
            HunkOutline: 0xFF3B82F6u,
            Selection: 0xFFCBD5E1u,
            Placeholder: WithAlpha(textMutedLight, 0x80),
            DialogHeaderSeparator: 0xFFE5E7EBu,
            CheckboxBorderIdle: 0xFFD1D5DBu,
            CheckboxDisabledFill: 0xFFE5E7EBu,
            SegmentActiveBg: 0xFF4F46E5u,
            ScrollBarTrackBg: 0xFFF3F4F6u,
            ScrollBarThumbBorder: 0xFFE5E7EBu,
            OperationRowHoverBg: 0xFFE5E7EBu,
            CommitRowSelectedBg: 0xFFE0E7FFu,
            CommitRowSelectedText: 0xFF111827u);

        var status = new StatusPalette(
            Success: 0xFF16A34Au,
            Warning: 0xFFCA8A04u,
            Danger: 0xFFDC2626u,
            Info: 0xFF2563EBu,
            Purple: 0xFFA855F7u,
            SuccessSoft: 0xFF16A34Au,
            WarningSoft: 0xFFEA580Cu,
            SuccessBar: 0xFF16A34Au,
            SuccessText: 0xFF166534u,
            SuccessLineBg: 0xFFDCFCE7u,
            SuccessLineGlyph: 0xFF15803Du,
            DangerBar: 0xFFDC2626u,
            DangerText: 0xFF7C2D12u,
            DangerLineBg: 0xFFFEE2E2u,
            DangerLineGlyph: 0xFFB91C1Cu,
            Other: 0xFF7C3AEDu,
            DialogError: 0xFFDC2626u,
            DiffError: 0xFF92400Eu);

        var banner = new BannerStyles(
            Background: 0xFFFEF3C7u,
            Border: 0xFFD97706u,
            Text: 0xFF78350Fu);

        var tooltip = new TooltipPalette(
            Background: p.TextSecondary,
            Border: p.TextBody,
            Text: p.TextOnAccent);

        var hunkButton = new DiffHunkButtonPalette(
            BackgroundIdle: WithAlpha(p.BorderStrong, 0xCC),
            BackgroundHover: p.SurfaceHoverStrong,
            Border: p.BorderStrong,
            Text: p.TextStrong);

        var commitBadge = new CommitBadgePalette(
            LocalBg: 0xFFDBEAFEu,
            RemoteBg: 0xFFEDE9FEu,
            HeadBg: 0xFFFEF3C7u,
            Text: p.TextBody);

        return BuildStyles(p, status, banner, tooltip, hunkButton, commitBadge);
    }

    private static ThemeStyles BuildStyles(
        ThemePalette p,
        StatusPalette status,
        BannerStyles banner,
        TooltipPalette tooltip,
        DiffHunkButtonPalette hunkButton,
        CommitBadgePalette commitBadge) =>
        new(
            Palette: p,
            Banner: banner,
            HeaderActionButton: new HeaderActionButtonStyles(
                Background: 0u,
                BackgroundHover: p.SurfaceHoverStrong,
                IconIdle: p.TextMedium,
                IconHover: p.TextStrong,
                IconDisabled: WithAlpha(p.TextMedium, 0x66)),
            LocalChangesContent: new LocalChangesContentStyles(
                ContentBackground: p.Surface,
                ColumnDivider: p.Border,
                PlaceholderText: p.TextMuted,
                SplitterIdle: p.Border,
                SplitterHover: p.BorderHoverFill),
            SubmoduleSection: new SubmoduleSectionStyles(
                BadgeBackground: status.Purple,
                BadgeText: p.OnStatusText,
                RowText: p.TextMedium),
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
                BadgeText: p.OnStatusText,
                StatusAdded: status.Success,
                StatusModified: status.Warning,
                StatusDeleted: status.Danger,
                StatusRenamed: status.Info,
                StatusConflicted: status.Danger,
                StatusSubmodule: status.Purple,
                StatusOther: status.Other),
            DialogFrame: new DialogFrameStyles(
                Background: p.Surface,
                Border: p.Border,
                TitleText: p.TextEmphasis,
                HeaderSeparator: p.DialogHeaderSeparator,
                ErrorText: status.DialogError,
                InsetBackground: p.SurfaceSunken),
            TextInput: new TextInputStyles(
                Background: p.InputSurface,
                Border: p.BorderStrong,
                Text: p.TextEmphasis,
                Caret: p.TextEmphasis,
                Selection: p.Selection,
                PlaceholderText: p.Placeholder),
            BorderedButton: new BorderedButtonStyles(
                BackgroundIdle: p.InputSurface,
                BackgroundHover: p.InputSurfaceHover,
                BorderIdle: p.BorderStrong,
                BorderHover: p.Accent,
                Text: p.TextStrong,
                TextDisabled: p.TextDisabled),
            DialogIconButton: new DialogIconButtonStyles(
                BackgroundIdle: 0u,
                BackgroundHover: p.SurfaceHoverStrong,
                TextIdle: p.TextSubtle,
                TextHover: p.TextStrong),
            ActionButton: new ActionButtonStyles(
                BackgroundIdle: 0u,
                BackgroundHover: p.SurfaceHoverStrong,
                TextIdle: p.TextSubtle,
                TextHover: p.TextStrong,
                TextDisabled: p.TextDisabled),
            Checkbox: new CheckboxStyles(
                TextIdle: p.TextSecondary,
                TextHover: p.TextStrong,
                TextDisabled: p.TextDisabled,
                BoxBorderIdle: p.CheckboxBorderIdle,
                BoxBorderHover: p.BorderMutedHover,
                BoxBorderDisabled: WithAlpha(p.CheckboxBorderIdle, 0x66),
                BoxFillChecked: p.Accent,
                BoxFillCheckedHover: p.AccentHover,
                BoxFillDisabled: p.CheckboxDisabledFill,
                CheckGlyph: p.TextOnAccent),
            CommitBar: new CommitBarStyles(
                Background: p.BarSurface,
                TopBorder: p.Border),
            ModeSwitcher: new ModeSwitcherStyles(
                PillBorder: p.BorderStrong,
                SegmentSeparator: p.BorderStrong,
                SegmentIdleBackground: 0u,
                SegmentHoverBackground: p.InputSurfaceHover,
                SegmentActiveBackground: p.SegmentActiveBg,
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
                Background: p.InputSurface,
                Border: p.Accent,
                Text: p.TextEmphasis,
                Caret: p.TextEmphasis,
                Selection: p.Selection),
            RepoBarRow: new RepoBarRowStyles(
                BackgroundIdle: 0u,
                BackgroundHover: p.SurfaceHover,
                BackgroundActive: p.SegmentActiveBg,
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
                RowTextActive: p.RowSubtleText,
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
                HeaderTitleIdle: p.TextSubtle,
                HeaderTitleHover: p.TextStrong),
            DiffContent: new DiffContentStyles(
                Background: p.Surface,
                PlaceholderText: p.TextMuted,
                ErrorText: status.DiffError,
                LineText: p.TextEmphasis,
                LineNumberText: p.TextFaint,
                LineAddedBackground: status.SuccessLineBg,
                LineAddedGlyph: status.SuccessLineGlyph,
                LineRemovedBackground: status.DangerLineBg,
                LineRemovedGlyph: status.DangerLineGlyph,
                LineContextGlyph: p.TextMuted,
                SectionBackground: p.SurfaceRaised,
                SectionMutedText: p.TextSecondary,
                HunkSeparatorRangeText: p.TextMuted,
                HunkOutline: p.HunkOutline),
            DiffHunkButton: new DiffHunkButtonStyles(
                BackgroundIdle: hunkButton.BackgroundIdle,
                BackgroundHover: hunkButton.BackgroundHover,
                Border: hunkButton.Border,
                Text: hunkButton.Text),
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
                TrackBackground: p.ScrollBarTrackBg,
                TrackBorder: p.Border,
                ThumbIdleBackground: p.BorderMuted,
                ThumbHoverBackground: p.BorderMutedHover,
                ThumbBorder: p.ScrollBarThumbBorder),
            Tooltip: new TooltipStyles(
                Background: tooltip.Background,
                Border: tooltip.Border,
                Text: tooltip.Text,
                Shadow: p.Shadow),
            OperationRow: new OperationRowStyles(
                IconText: p.TextEmphasis,
                LabelText: p.TextEmphasis,
                PhaseTextIdle: p.TextFaint,
                ElapsedText: p.TextFaint,
                BackgroundIdle: p.BarSurface,
                BackgroundHover: p.OperationRowHoverBg,
                SuccessBar: status.SuccessBar,
                SuccessText: status.SuccessText,
                FailureBar: status.DangerBar,
                FailureText: status.DangerText),
            CommitDetailsView: new CommitDetailsViewStyles(
                Background: p.SurfaceSunken,
                BorderLeft: p.Border,
                PrimaryText: p.TextEmphasis,
                SecondaryText: p.TextSecondary,
                MutedText: p.TextFaint,
                PlaceholderText: p.TextMuted,
                SplitterIdle: p.Border,
                SplitterHover: p.BorderHoverFill),
            DialogBody: new DialogBodyStyles(
                BodyText: p.TextBody,
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
                ContainerBackground: p.BarSurface,
                ContainerBorder: p.Border,
                LogBackground: p.SurfaceSunken,
                LogBorder: p.Border,
                LogText: p.TextSecondary),
            CommitsView: new CommitsViewStyles(
                Background: p.Surface,
                HeaderBackground: p.BarSurface,
                HeaderBorderBottom: p.Border,
                HeaderText: p.TextMuted,
                RowText: p.TextSecondary,
                RowTextActive: p.CommitRowSelectedText,
                RowTextDim: p.TextDim,
                RowSelectedBackground: p.CommitRowSelectedBg,
                PlaceholderText: p.TextMuted,
                ColumnDividerIdle: p.Border,
                ColumnDividerHoverFill: p.BorderHoverFill,
                ColumnDividerHoverLine: p.BorderHoverLine,
                BadgeLocalBackground: commitBadge.LocalBg,
                BadgeRemoteBackground: commitBadge.RemoteBg,
                BadgeHeadBackground: commitBadge.HeadBg,
                BadgeText: commitBadge.Text));
}
