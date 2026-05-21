using ZGF.Gui;
using ZGF.Gui.Layouts;
using ZGF.Gui.Tests;
using ZGF.KeyboardModule;
using ZGF.Observable;

namespace GitGui;

/// <summary>
/// Confirmation modal shown when the user picks "Reset … to here" on a commit and the
/// working tree has local changes. Each mode preserves a different slice of the dirty
/// state — Soft keeps both index and worktree, Mixed keeps only the worktree, Hard
/// throws everything away — so the user picks explicitly rather than us guessing.
/// </summary>
public sealed class ResetCommitDialog : MultiChildView, IResetCommitView
{
    private const float CloseButtonSize = 28f;

    private readonly Action _onClose;
    private readonly ResetModeDropdown _modeDropdown;
    private readonly DialogButton _resetButton;
    private readonly TextView _errorView;

    public event Action<ResetMode>? ResetRequested;

    public ResetCommitDialog(Repo repo, string sha, string shortSha, int stagedCount, int unstagedCount, Action onClose)
    {
        PreferredWidth = 520f;

        _onClose = onClose;

        var title = new TextView
        {
            Text = $"Reset to {shortSha}",
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

        var prompt = new TextView
        {
            Text = BuildPrompt(stagedCount, unstagedCount),
            TextColor = DialogPalette.BodyText,
            TextWrap = TextWrap.Wrap,
        };

        _modeDropdown = new ResetModeDropdown();
        var modeRow = BuildLabeledRow("Mode:", _modeDropdown);

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
                        prompt,
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
            PreferredWidth = 110,
            MainAxisAlignment = MainAxisAlignment.End,
            CrossAxisAlignment = CrossAxisAlignment.Center,
            Children = { labelText },
        };
        return new FlexRowView
        {
            Gap = 10,
            CrossAxisAlignment = CrossAxisAlignment.Center,
            PreferredHeight = 28,
            Children =
            {
                labelColumn,
                new FlexItem { Grow = 1, Child = value },
            },
        };
    }

    private static string BuildPrompt(int staged, int unstaged)
    {
        var parts = new List<string>();
        if (staged > 0) parts.Add($"{staged} staged");
        if (unstaged > 0) parts.Add($"{unstaged} unstaged");
        var summary = parts.Count > 0 ? string.Join(" and ", parts) : "local changes";
        return $"You have {summary} change(s). Choose how the reset should treat them:";
    }
}

internal sealed class ResetModeDropdown : HoverableButton
{
    private static readonly (ResetMode Mode, string Label, string Detail)[] Options =
    {
        (ResetMode.Mixed, "Mixed", "Keep working tree; changes appear unstaged"),
        (ResetMode.Soft, "Soft", "Keep working tree and index; changes appear staged"),
        (ResetMode.Hard, "Hard", "Discard all local changes"),
    };

    private readonly TextView _labelView;
    private readonly TextView _detailView;
    public State<ResetMode> SelectedState { get; } = new(ResetMode.Mixed);

    public ResetMode Selected => SelectedState.Value;

    public ResetModeDropdown()
    {
        PreferredHeight = 30;
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

        var row = new FlexRowView
        {
            Gap = 10,
            CrossAxisAlignment = CrossAxisAlignment.Center,
            Children =
            {
                _labelView,
                new FlexItem { Grow = 1, Child = _detailView },
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
                () => SelectedState.Value = mode));
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
