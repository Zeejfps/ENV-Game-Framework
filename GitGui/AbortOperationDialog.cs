using ZGF.Gui;
using ZGF.Gui.Layouts;
using ZGF.Gui.Tests;
using ZGF.KeyboardModule;
using ZGF.Observable;

namespace GitGui;

/// <summary>
/// Confirmation modal for aborting an in-progress op (merge / rebase / cherry-pick /
/// revert / am / bisect) or recovering from a stash-apply conflict via `git reset --merge`.
/// All variants are destructive — any in-progress conflict resolutions and (for
/// reset --merge) conflicting worktree edits are thrown away — so the user confirms first.
/// </summary>
public sealed class AbortOperationDialog : MultiChildView, IAbortOperationView
{
    private readonly Action _onClose;
    private readonly DialogButton _abortButton;
    private readonly DialogButton _cancelButton;
    private readonly TextView _errorView;

    public event Action? AbortRequested;

    public AbortOperationDialog(Repo repo, RepoOperationState state, Action onClose)
    {
        PreferredWidth = 480f;
        PreferredHeight = 240f;

        _onClose = onClose;

        var (titleText, bodyText, confirmLabel) = CopyFor(state);

        var prompt = new TextView
        {
            Text = bodyText,
            TextColor = DialogPalette.BodyText,
            TextWrap = TextWrap.Wrap,
        };

        _errorView = DialogFrame.ErrorView();

        _cancelButton = new DialogButton("Cancel", onClose) { PreferredHeight = DialogFrame.DefaultButtonHeight };
        _abortButton = new DialogButton(confirmLabel, RaiseAbortRequested) { PreferredHeight = DialogFrame.DefaultButtonHeight };

        AddChildToSelf(DialogFrame.Build(titleText, onClose, new FlexColumnView
        {
            Gap = 12,
            CrossAxisAlignment = CrossAxisAlignment.Stretch,
            Children =
            {
                new FlexItem { Grow = 1, Child = prompt },
                _errorView,
                DialogFrame.ButtonsRow(_cancelButton, _abortButton),
            },
        }));

        this.UseController(_ => new AbortOperationKbmController(RaiseAbortRequested, onClose));

        var request = new AbortOperationRequest(repo, state);
        this.UsePresenter(ctx => new AbortOperationPresenter(
            this, request,
            ctx.Require<IGitService>(),
            ctx.Require<IUiDispatcher>(),
            ctx.Require<IMessageBus>()));
    }

    public bool AbortEnabled
    {
        set => _abortButton.IsEnabled.Value = value;
    }

    public bool CancelEnabled
    {
        set => _cancelButton.IsEnabled.Value = value;
    }

    public string? ErrorMessage
    {
        set => _errorView.Text = value ?? string.Empty;
    }

    public string ConfirmButtonLabel
    {
        set => _abortButton.Label = value;
    }

    public bool IsBusy
    {
        set
        {
            _abortButton.Icon = value ? LucideIcons.Loader : string.Empty;
            if (!value) _abortButton.IconRotation = 0f;
        }
    }

    public float BusyRotation
    {
        set => _abortButton.IconRotation = value;
    }

    public void Close() => _onClose();

    private void RaiseAbortRequested() => AbortRequested?.Invoke();

    private static (string Title, string Body, string Confirm) CopyFor(RepoOperationState state) => state switch
    {
        RepoOperationState.Merge => (
            "Abort merge?",
            "Aborts the in-progress merge and restores the working tree to the pre-merge state. Any conflict resolutions you've made will be lost.",
            "Abort merge"),
        RepoOperationState.Rebase => (
            "Abort rebase?",
            "Aborts the in-progress rebase and returns HEAD to the branch's original tip. Any conflict resolutions you've made will be lost.",
            "Abort rebase"),
        RepoOperationState.CherryPick => (
            "Abort cherry-pick?",
            "Aborts the in-progress cherry-pick and restores the working tree to the pre-cherry-pick state.",
            "Abort cherry-pick"),
        RepoOperationState.Revert => (
            "Abort revert?",
            "Aborts the in-progress revert and restores the working tree to the pre-revert state.",
            "Abort revert"),
        RepoOperationState.ApplyMailbox => (
            "Abort patch apply?",
            "Aborts the in-progress `git am` and restores the working tree to the pre-apply state. The mailbox queue is discarded.",
            "Abort apply"),
        RepoOperationState.Bisect => (
            "Reset bisect?",
            "Ends the bisect session and returns HEAD to where it was when bisect started.",
            "Reset bisect"),
        RepoOperationState.UnmergedPaths => (
            "Reset unmerged paths?",
            "Discards conflicting worktree changes and clears the unmerged index entries, returning the conflicted files to HEAD. Clean local changes are kept; in-progress conflict resolutions are lost.",
            "Reset"),
        _ => ("Abort?", "Cancel the in-progress operation.", "Abort"),
    };
}

internal sealed class AbortOperationKbmController : KeyboardMouseController
{
    private readonly Action _onConfirm;
    private readonly Action _onCancel;

    public AbortOperationKbmController(Action onConfirm, Action onCancel)
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

public readonly record struct AbortOperationRequest(Repo Repo, RepoOperationState State);
