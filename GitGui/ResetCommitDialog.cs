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
    private readonly DialogButton _softButton;
    private readonly DialogButton _mixedButton;
    private readonly DialogButton _hardButton;
    private readonly TextView _errorView;

    public event Action<ResetMode>? ResetRequested;

    public ResetCommitDialog(Repo repo, string sha, string shortSha, int stagedCount, int unstagedCount, Action onClose)
    {
        PreferredWidth = 520f;
        PreferredHeight = 340f;

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

        _softButton = BuildModeButton(
            "Soft",
            "Keep working tree and index; changes appear as staged.",
            () => RaiseResetRequested(ResetMode.Soft));
        _mixedButton = BuildModeButton(
            "Mixed",
            "Keep working tree; changes appear as unstaged.",
            () => RaiseResetRequested(ResetMode.Mixed));
        _hardButton = BuildModeButton(
            "Hard",
            "Discard all local changes. Cannot be undone.",
            () => RaiseResetRequested(ResetMode.Hard));

        _errorView = new TextView
        {
            Text = string.Empty,
            TextColor = 0xFFE06C75,
            TextWrap = TextWrap.Wrap,
        };

        var cancelButton = new DialogButton("Cancel", onClose)
        {
            PreferredHeight = 32,
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
                        _softButton,
                        _mixedButton,
                        _hardButton,
                        _errorView,
                        new FlexItem { Grow = 1, Child = new MultiChildView() },
                        cancelButton,
                    },
                },
            },
        });

        this.UseController(_ => new ResetCommitKbmController(onClose));

        var request = new ResetCommitRequest(repo, sha);
        this.UsePresenter(ctx => new ResetCommitPresenter(
            this, request,
            ctx.Require<IGitService>(),
            ctx.Require<IUiDispatcher>(),
            ctx.Require<IMessageBus>()));
    }

    public bool ButtonsEnabled
    {
        set
        {
            _softButton.IsEnabled.Value = value;
            _mixedButton.IsEnabled.Value = value;
            _hardButton.IsEnabled.Value = value;
        }
    }

    public string? ErrorMessage
    {
        set => _errorView.Text = value ?? string.Empty;
    }

    public void Close() => _onClose();

    private void RaiseResetRequested(ResetMode mode) => ResetRequested?.Invoke(mode);

    private static DialogButton BuildModeButton(string label, string detail, Action onClick)
        => new($"{label}  —  {detail}", onClick)
        {
            PreferredHeight = 40,
        };

    private static string BuildPrompt(int staged, int unstaged)
    {
        var parts = new List<string>();
        if (staged > 0) parts.Add($"{staged} staged");
        if (unstaged > 0) parts.Add($"{unstaged} unstaged");
        var summary = parts.Count > 0 ? string.Join(" and ", parts) : "local changes";
        return $"You have {summary} change(s). Choose how the reset should treat them:";
    }
}

internal sealed class ResetCommitKbmController : KeyboardMouseController
{
    private readonly Action _onCancel;

    public ResetCommitKbmController(Action onCancel)
    {
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
    }
}
