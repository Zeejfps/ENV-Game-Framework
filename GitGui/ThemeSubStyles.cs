namespace GitGui;

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
    uint TextBody,
    uint TextSecondary,
    uint TextMedium,
    uint TextMuted,
    uint TextDim,
    uint TextDisabled,
    uint TextOnAccent,
    uint Shadow,
    uint BarSurface,
    uint InputSurface,
    uint InputSurfaceHover,
    uint TextEmphasis,
    uint TextSubtle,
    uint TextFaint,
    uint OnStatusText,
    uint RowSubtleText,
    uint HunkOutline,
    uint Selection,
    uint Placeholder,
    uint DialogHeaderSeparator,
    uint CheckboxBorderIdle,
    uint CheckboxDisabledFill,
    uint SegmentActiveBg,
    uint ScrollBarTrackBg,
    uint ScrollBarThumbBorder,
    uint OperationRowHoverBg,
    uint CommitRowSelectedBg,
    uint CommitRowSelectedText);

public sealed record StatusPalette(
    uint Success,
    uint Warning,
    uint Danger,
    uint Info,
    uint Purple,
    uint SuccessSoft,
    uint WarningSoft,
    uint SuccessBar,
    uint SuccessText,
    uint SuccessLineBg,
    uint SuccessLineGlyph,
    uint DangerBar,
    uint DangerText,
    uint DangerLineBg,
    uint DangerLineGlyph,
    uint Other,
    uint DialogError,
    uint DiffError);

public sealed record TooltipPalette(
    uint Background,
    uint Border,
    uint Text);

public sealed record DiffHunkButtonPalette(
    uint BackgroundIdle,
    uint BackgroundHover,
    uint Border,
    uint Text);

public sealed record CommitBadgePalette(
    uint LocalBg,
    uint RemoteBg,
    uint HeadBg,
    uint Text);

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
