using ZGF.Gui;
using ZGF.Gui.Layouts;
using ZGF.Gui.Tests;
using ZGF.KeyboardModule;
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
    private const float CloseButtonSize = 28f;

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

        var title = new TextView
        {
            Text = "Reset to revision",
            TextColor = DialogPalette.TitleText,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
        };

        var headerRow = new FlexRowView
        {
            CrossAxisAlignment = CrossAxisAlignment.Center,
            PreferredHeight = 28,
            Children =
            {
                new MultiChildView { PreferredWidth = CloseButtonSize },
                new FlexItem { Grow = 1, Child = title },
                new DialogCloseButton(onClose),
            },
        };

        var subtitle = new TextView
        {
            Text = branchName != null
                ? $"Move the '{branchName}' branch HEAD to the selected revision"
                : "Move HEAD to the selected revision",
            TextColor = DialogPalette.BodyText,
            TextWrap = TextWrap.Wrap,
        };

        var dirtyHint = new TextView
        {
            Text = BuildDirtyHint(stagedCount, unstagedCount),
            TextColor = DialogPalette.RowTextMissing,
            TextWrap = TextWrap.Wrap,
        };

        var branchRow = BuildLabeledRow("Branch:", BuildBranchValue(branchName));
        var moveToRow = BuildLabeledRow("Move to:", BuildCommitValue(shortSha, summary));
        _modeDropdown = new ResetModeDropdown();
        var modeRow = BuildLabeledRow("Reset type:", _modeDropdown);

        _errorView = new TextView
        {
            Text = string.Empty,
            TextColor = 0xFFE06C75,
            TextWrap = TextWrap.Wrap,
        };

        var cancelButton = new DialogButton("Cancel", onClose) { PreferredHeight = 32, PreferredWidth = 96 };
        _resetButton = new DialogButton("Reset", RaiseResetRequested) { PreferredHeight = 32, PreferredWidth = 96 };

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

        AddChildToSelf(new RectView
        {
            BackgroundColor = DialogPalette.Background,
            BorderColor = BorderColorStyle.All(DialogPalette.Border),
            BorderSize = BorderSizeStyle.All(1),
            BorderRadius = BorderRadiusStyle.All(10),
            Padding = PaddingStyle.All(20),
            Children =
            {
                new FlexColumnView
                {
                    Gap = 12,
                    CrossAxisAlignment = CrossAxisAlignment.Stretch,
                    Children =
                    {
                        headerRow,
                        new RectView
                        {
                            BackgroundColor = DialogPalette.Separator,
                            PreferredHeight = 1,
                        },
                        subtitle,
                        dirtyHint,
                        branchRow,
                        moveToRow,
                        modeRow,
                        _errorView,
                        new MultiChildView { PreferredHeight = 4 },
                        buttonsRow,
                    },
                },
            },
        });

        this.UseController(_ => new ResetCommitKbmController(RaiseResetRequested, onClose));

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
            TextColor = DialogPalette.SectionHeaderText,
            VerticalTextAlignment = TextAlignment.Center,
        };
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
            TextColor = DialogPalette.BodyText,
            VerticalTextAlignment = TextAlignment.Center,
            PreferredWidth = 16,
        };
        var label = new TextView
        {
            Text = branchName ?? "(detached HEAD)",
            TextColor = DialogPalette.TitleText,
            VerticalTextAlignment = TextAlignment.Center,
        };
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
            TextColor = DialogPalette.BodyText,
            VerticalTextAlignment = TextAlignment.Center,
            HorizontalTextAlignment = TextAlignment.Center,
            PreferredWidth = 16,
        };
        var shaLabel = new TextView
        {
            Text = shortSha,
            TextColor = DialogPalette.TitleText,
            VerticalTextAlignment = TextAlignment.Center,
        };
        var summaryLabel = new TextView
        {
            Text = summary,
            TextColor = DialogPalette.BodyText,
            VerticalTextAlignment = TextAlignment.Center,
            TextWrap = TextWrap.NoWrap,
        };
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
            TextColor = DialogPalette.TitleText,
            VerticalTextAlignment = TextAlignment.Center,
        };
        _detailView = new TextView
        {
            Text = LookupDetail(ResetMode.Mixed),
            TextColor = DialogPalette.RowTextMissing,
            VerticalTextAlignment = TextAlignment.Center,
            TextWrap = TextWrap.NoWrap,
        };
        var chevron = new TextView
        {
            Text = LucideIcons.ChevronDown,
            FontFamily = LucideIcons.FontFamily,
            FontSize = 12,
            TextColor = DialogPalette.RowText,
            VerticalTextAlignment = TextAlignment.Center,
            HorizontalTextAlignment = TextAlignment.Center,
            PreferredWidth = 16,
        };

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

internal sealed class ResetCommitKbmController : KeyboardMouseController
{
    private readonly Action _onConfirm;
    private readonly Action _onCancel;

    public ResetCommitKbmController(Action onConfirm, Action onCancel)
    {
        _onConfirm = onConfirm;
        _onCancel = onCancel;
    }

    public override void OnKeyboardKeyStateChanged(ref KeyboardKeyEvent e)
    {
        if (e.State != InputState.Pressed) return;
        if (e.Key == KeyboardKey.Escape)
        {
            e.Consume();
            _onCancel();
        }
        else if (e.Key == KeyboardKey.Enter || e.Key == KeyboardKey.NumpadEnter)
        {
            e.Consume();
            _onConfirm();
        }
    }
}
