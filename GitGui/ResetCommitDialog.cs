using ZGF.Gui;
using ZGF.Gui.Layouts;
using ZGF.Gui.Tests;
using ZGF.Observable;

namespace GitGui;

/// <summary>
/// Confirmation modal shown when the user picks "Reset … to here" on a commit and the
/// working tree has local changes. Mirrors Fork's layout: a "Branch:" / "Move to:" /
/// "Reset type:" stack, with the reset mode picked via a coloured-dot dropdown (green
/// soft, amber mixed, red hard) so the destructiveness reads at a glance.
/// </summary>
public sealed class ResetCommitDialog : MultiChildView, IResetCommitView
{
    internal const uint SoftColor = 0xFF57F287;
    internal const uint MixedColor = 0xFFE6A85C;
    internal const uint HardColor = 0xFFED4245;

    private readonly Action _onClose;
    private readonly ResetModeDropdown _modeDropdown;
    private readonly DialogButton _resetButton;
    private readonly TextView _errorView;

    public event Action<ResetMode>? ResetRequested;

    public ResetCommitDialog(
        Repo repo,
        string sha,
        string shortSha,
        string summary,
        string? branchName,
        int stagedCount,
        int unstagedCount,
        Action onClose)
    {
        PreferredWidth = 560f;

        _onClose = onClose;

        var subtitle = new TextView
        {
            Text = branchName != null
                ? $"Move the '{branchName}' branch HEAD to the selected revision"
                : "Move HEAD to the selected revision",
            TextWrap = TextWrap.Wrap,
        };
        subtitle.BindTextColorFromTheme(t => t.Dialog.BodyText);

        var dirtyHint = new TextView
        {
            Text = BuildDirtyHint(stagedCount, unstagedCount),
            TextWrap = TextWrap.Wrap,
        };
        dirtyHint.BindTextColorFromTheme(t => t.Dialog.RowTextMissing);

        var branchRow = BuildLabeledRow("Branch:", BuildBranchValue(branchName));
        var moveToRow = BuildLabeledRow("Move to:", BuildCommitValue(shortSha, summary));
        _modeDropdown = new ResetModeDropdown();
        var modeRow = BuildLabeledRow("Reset type:", _modeDropdown);

        _errorView = DialogFrame.ErrorView();

        var cancelButton = new DialogButton("Cancel", onClose) { PreferredHeight = DialogFrame.DefaultButtonHeight, PreferredWidth = 96 };
        _resetButton = new DialogButton("Reset", RaiseResetRequested) { PreferredHeight = DialogFrame.DefaultButtonHeight, PreferredWidth = 96 };

        var buttonsRow = new FlexRowView
        {
            Gap = 8,
            CrossAxisAlignment = CrossAxisAlignment.Center,
            Children =
            {
                new FlexItem { Grow = 1, Child = new MultiChildView() },
                cancelButton,
                _resetButton,
            },
        };

        AddChildToSelf(DialogFrame.Build("Reset to revision", onClose, new FlexColumnView
        {
            Gap = 12,
            CrossAxisAlignment = CrossAxisAlignment.Stretch,
            Children =
            {
                subtitle,
                dirtyHint,
                branchRow,
                moveToRow,
                modeRow,
                _errorView,
                new MultiChildView { PreferredHeight = 4 },
                buttonsRow,
            },
        }));

        this.UseController(_ => new DialogKbmController(RaiseResetRequested, onClose));

        var request = new ResetCommitRequest(repo, sha);
        this.UsePresenter(ctx => new ResetCommitPresenter(
            this, request,
            ctx.Require<IGitService>(),
            ctx.Require<IUiDispatcher>(),
            ctx.Require<IMessageBus>()));
    }

    public bool ButtonsEnabled
    {
        set => _resetButton.IsEnabled.Value = value;
    }

    public string? ErrorMessage
    {
        set => _errorView.Text = value ?? string.Empty;
    }

    public void Close() => _onClose();

    private void RaiseResetRequested() => ResetRequested?.Invoke(_modeDropdown.Selected);

    private static FlexRowView BuildLabeledRow(string label, MultiChildView value)
    {
        var labelText = new TextView
        {
            Text = label,
            VerticalTextAlignment = TextAlignment.Center,
        };
        labelText.BindTextColorFromTheme(t => t.Dialog.SectionHeaderText);
        var labelColumn = new FlexRowView
        {
            PreferredWidth = 90,
            MainAxisAlignment = MainAxisAlignment.End,
            CrossAxisAlignment = CrossAxisAlignment.Center,
            Children = { labelText },
        };
        return new FlexRowView
        {
            Gap = 10,
            CrossAxisAlignment = CrossAxisAlignment.Center,
            PreferredHeight = 30,
            Children =
            {
                labelColumn,
                new FlexItem { Grow = 1, Child = value },
            },
        };
    }

    private static MultiChildView BuildBranchValue(string? branchName)
    {
        var icon = new TextView
        {
            Text = LucideIcons.Branch,
            FontFamily = LucideIcons.FontFamily,
            FontSize = 14,
            VerticalTextAlignment = TextAlignment.Center,
            PreferredWidth = 16,
        };
        icon.BindTextColorFromTheme(t => t.Dialog.BodyText);

        var label = new TextView
        {
            Text = branchName ?? "(detached HEAD)",
            VerticalTextAlignment = TextAlignment.Center,
        };
        label.BindTextColorFromTheme(t => t.Dialog.TitleText);
        return new FlexRowView
        {
            Gap = 6,
            CrossAxisAlignment = CrossAxisAlignment.Center,
            Children = { icon, label },
        };
    }

    private static MultiChildView BuildCommitValue(string shortSha, string summary)
    {
        var dot = new TextView
        {
            Text = "●",
            FontSize = 10,
            VerticalTextAlignment = TextAlignment.Center,
            HorizontalTextAlignment = TextAlignment.Center,
            PreferredWidth = 16,
        };
        dot.BindTextColorFromTheme(t => t.Dialog.BodyText);

        var shaLabel = new TextView
        {
            Text = shortSha,
            VerticalTextAlignment = TextAlignment.Center,
        };
        shaLabel.BindTextColorFromTheme(t => t.Dialog.TitleText);

        var summaryLabel = new TextView
        {
            Text = summary,
            VerticalTextAlignment = TextAlignment.Center,
            TextWrap = TextWrap.NoWrap,
        };
        summaryLabel.BindTextColorFromTheme(t => t.Dialog.BodyText);
        var summaryClip = new ClippingView
        {
            Children = { summaryLabel },
        };
        return new FlexRowView
        {
            Gap = 8,
            CrossAxisAlignment = CrossAxisAlignment.Center,
            Children =
            {
                dot,
                shaLabel,
                new FlexItem { Grow = 1, Child = summaryClip },
            },
        };
    }

    private static string BuildDirtyHint(int staged, int unstaged)
    {
        var parts = new List<string>();
        if (staged > 0) parts.Add($"{staged} staged");
        if (unstaged > 0) parts.Add($"{unstaged} unstaged");
        if (parts.Count == 0) return string.Empty;
        return $"You have {string.Join(" and ", parts)} local change(s).";
    }
}

internal sealed class ResetModeDropdown : HoverableButton
{
    // Order: safest → most destructive (Fork uses Soft / Mixed / Hard top-to-bottom).
    private static readonly (ResetMode Mode, string Label, string Detail, uint Color)[] Options =
    {
        (ResetMode.Soft, "Soft", "Keep all changes. Stage differences", ResetCommitDialog.SoftColor),
        (ResetMode.Mixed, "Mixed", "Keep all changes. Unstage differences", ResetCommitDialog.MixedColor),
        (ResetMode.Hard, "Hard", "Discard all local changes", ResetCommitDialog.HardColor),
    };

    // Default Mixed: matches git's own default and is the safest non-destructive option
    // that still discards the staged-vs-unstaged distinction (which the user is
    // re-considering anyway by resetting).
    public State<ResetMode> SelectedState { get; } = new(ResetMode.Mixed);

    public ResetMode Selected => SelectedState.Value;

    private readonly TextView _dotView;
    private readonly TextView _labelView;
    private readonly TextView _detailView;

    public ResetModeDropdown()
    {
        PreferredHeight = 30;

        _dotView = new TextView
        {
            Text = "●",
            FontSize = 12,
            TextColor = LookupColor(ResetMode.Mixed),
            VerticalTextAlignment = TextAlignment.Center,
            HorizontalTextAlignment = TextAlignment.Center,
            PreferredWidth = 14,
        };
        _labelView = new TextView
        {
            Text = LookupLabel(ResetMode.Mixed),
            VerticalTextAlignment = TextAlignment.Center,
        };
        _labelView.BindTextColorFromTheme(t => t.Dialog.TitleText);

        _detailView = new TextView
        {
            Text = LookupDetail(ResetMode.Mixed),
            VerticalTextAlignment = TextAlignment.Center,
            TextWrap = TextWrap.NoWrap,
        };
        _detailView.BindTextColorFromTheme(t => t.Dialog.RowTextMissing);

        var chevron = new TextView
        {
            Text = LucideIcons.ChevronDown,
            FontFamily = LucideIcons.FontFamily,
            FontSize = 12,
            VerticalTextAlignment = TextAlignment.Center,
            HorizontalTextAlignment = TextAlignment.Center,
            PreferredWidth = 16,
        };
        chevron.BindTextColorFromTheme(t => t.Dialog.RowText);

        // Wrapping the detail in a ClippingView keeps long descriptions from overflowing
        // past the chevron — the framework's TextView doesn't clip on its own.
        var detailClip = new ClippingView
        {
            Children = { _detailView },
        };

        var row = new FlexRowView
        {
            Gap = 8,
            CrossAxisAlignment = CrossAxisAlignment.Center,
            Children =
            {
                _dotView,
                _labelView,
                new FlexItem { Grow = 1, Child = detailClip },
                chevron,
            },
        };

        var background = new RectView
        {
            BorderSize = BorderSizeStyle.All(1),
            BorderRadius = BorderRadiusStyle.All(3),
            Padding = new PaddingStyle { Left = 8, Right = 8, Top = 4, Bottom = 4 },
            Children = { row },
        };
        DialogPalette.BindBorderedButtonChrome(background, IsHovered);
        SetBackground(background);

        SelectedState.Subscribe(s =>
        {
            _dotView.TextColor = LookupColor(s);
            _labelView.Text = LookupLabel(s);
            _detailView.Text = LookupDetail(s);
        });
    }

    protected override void OnClicked()
    {
        var ctx = Context;
        if (ctx == null) return;
        var items = new List<RepoBarContextMenu.Item>(Options.Length);
        foreach (var opt in Options)
        {
            var mode = opt.Mode;
            items.Add(new RepoBarContextMenu.Item(
                $"{opt.Label} — {opt.Detail}",
                () => SelectedState.Value = mode,
                LabelSegments: new[]
                {
                    new MenuLabelSegment("● ", opt.Color),
                    new MenuLabelSegment(opt.Label, Bold: true),
                    new MenuLabelSegment("  " + opt.Detail),
                }));
        }
        RepoBarContextMenu.Show(ctx, Position.BottomLeft, items);
    }

    private static string LookupLabel(ResetMode m)
    {
        foreach (var o in Options) if (o.Mode == m) return o.Label;
        return string.Empty;
    }

    private static string LookupDetail(ResetMode m)
    {
        foreach (var o in Options) if (o.Mode == m) return o.Detail;
        return string.Empty;
    }

    private static uint LookupColor(ResetMode m)
    {
        foreach (var o in Options) if (o.Mode == m) return o.Color;
        return ResetCommitDialog.MixedColor;
    }
}

