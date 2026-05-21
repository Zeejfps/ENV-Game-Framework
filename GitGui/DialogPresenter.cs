using ZGF.Gui;

namespace GitGui;

public sealed class DialogPresenter : IViewBehavior
{
    private readonly DialogSurfaceView _dialogSurfaceView;

    public DialogPresenter(DialogSurfaceView dialogSurfaceView)
    {
        _dialogSurfaceView = dialogSurfaceView;
    }

    public void AttachToContext(View view, Context context)
    {
        var _messageBus = context.Get<IMessageBus>();
        _messageBus?.Subscribe<AddRepoMessage>(OnAddRepoMessageReceived);
        _messageBus?.Subscribe<ShowCheckoutDialogMessage>(OnShowCheckoutDialog);
        _messageBus?.Subscribe<ShowOperationErrorMessage>(OnShowOperationError);
        _messageBus?.Subscribe<ShowDiscardChangesDialogMessage>(OnShowDiscardChangesDialog);
        _messageBus?.Subscribe<ShowCreateBranchDialogMessage>(OnShowCreateBranchDialog);
        _messageBus?.Subscribe<ShowRenameBranchDialogMessage>(OnShowRenameBranchDialog);
        _messageBus?.Subscribe<ShowDeleteLocalBranchDialogMessage>(OnShowDeleteLocalBranchDialog);
        _messageBus?.Subscribe<ShowDeleteRemoteBranchDialogMessage>(OnShowDeleteRemoteBranchDialog);
        _messageBus?.Subscribe<ShowStashDialogMessage>(OnShowStashDialog);
        _messageBus?.Subscribe<ShowDropStashDialogMessage>(OnShowDropStashDialog);
        _messageBus?.Subscribe<ShowAbortOperationDialogMessage>(OnShowAbortOperationDialog);
        _messageBus?.Subscribe<ShowCreateWorktreeDialogMessage>(OnShowCreateWorktreeDialog);
        _messageBus?.Subscribe<ShowRemoveWorktreeDialogMessage>(OnShowRemoveWorktreeDialog);
        _messageBus?.Subscribe<ShowMergeBranchDialogMessage>(OnShowMergeBranchDialog);
        _messageBus?.Subscribe<ShowRebaseBranchDialogMessage>(OnShowRebaseBranchDialog);
        _messageBus?.Subscribe<ShowPublishBranchDialogMessage>(OnShowPublishBranchDialog);
        _messageBus?.Subscribe<ShowForcePushDialogMessage>(OnShowForcePushDialog);
        _messageBus?.Subscribe<ShowAddSubmoduleDialogMessage>(OnShowAddSubmoduleDialog);
        _messageBus?.Subscribe<ShowUpdateSubmodulesDialogMessage>(OnShowUpdateSubmodulesDialog);
        _messageBus?.Subscribe<ShowDeinitSubmoduleDialogMessage>(OnShowDeinitSubmoduleDialog);
    }

    public void DetachFromContext(View view, Context context)
    {
    }

    private void OnAddRepoMessageReceived(AddRepoMessage _)
        => ShowDialog(new AddRepoDialog(OnDialogClosed));

    private void OnShowCheckoutDialog(ShowCheckoutDialogMessage m)
        => ShowDialog(
            new CheckoutBranchDialog(m.Repo, m.RemoteName, m.RemoteBranchName, m.SuggestedLocalName, OnDialogClosed));

    private void OnShowOperationError(ShowOperationErrorMessage m)
    {
        // Defensive: a blank body means no actionable info — showing the chrome alone is
        // worse than dropping the message. Callers should produce a real message (the
        // git CLI almost always writes *something* on failure); this guard is a backstop
        // for the few paths where we couldn't extract any meaningful text.
        if (string.IsNullOrWhiteSpace(m.Message)) return;
        ShowDialog(new OperationErrorDialog(m.Title, m.Message, OnDialogClosed));
    }

    private void OnShowDiscardChangesDialog(ShowDiscardChangesDialogMessage m)
        => ShowDialog(new DiscardChangesDialog(m.Repo, m.Paths, OnDialogClosed));

    private void OnShowCreateBranchDialog(ShowCreateBranchDialogMessage m)
        => ShowDialog(new CreateBranchDialog(m.Repo, m.SuggestedStartPoint, OnDialogClosed));

    private void OnShowRenameBranchDialog(ShowRenameBranchDialogMessage m)
        => ShowDialog(new RenameBranchDialog(m.Repo, m.CurrentName, OnDialogClosed));

    private void OnShowDeleteLocalBranchDialog(ShowDeleteLocalBranchDialogMessage m)
        => ShowDialog(new DeleteLocalBranchDialog(m.Repo, m.BranchName, OnDialogClosed));

    private void OnShowDeleteRemoteBranchDialog(ShowDeleteRemoteBranchDialogMessage m)
        => ShowDialog(new DeleteRemoteBranchDialog(m.Repo, m.RemoteName, m.BranchName, OnDialogClosed));

    private void OnShowStashDialog(ShowStashDialogMessage m)
        => ShowDialog(new StashDialog(m.Repo, OnDialogClosed));

    private void OnShowDropStashDialog(ShowDropStashDialogMessage m)
        => ShowDialog(new DropStashDialog(m.Repo, m.Index, m.Label, m.Subject, OnDialogClosed));

    private void OnShowAbortOperationDialog(ShowAbortOperationDialogMessage m)
        => ShowDialog(new AbortOperationDialog(m.Repo, m.State, OnDialogClosed));

    private void OnShowCreateWorktreeDialog(ShowCreateWorktreeDialogMessage m)
        => ShowDialog(new CreateWorktreeDialog(m.Primary, OnDialogClosed));

    private void OnShowRemoveWorktreeDialog(ShowRemoveWorktreeDialogMessage m)
        => ShowDialog(new RemoveWorktreeDialog(m.Primary, m.Worktree, OnDialogClosed));

    private void OnShowMergeBranchDialog(ShowMergeBranchDialogMessage m)
        => ShowDialog(new MergeBranchDialog(
            new MergeBranchRequest(m.Repo, m.SourceRef, m.SourceDisplay, m.TargetBranch),
            OnDialogClosed));

    private void OnShowRebaseBranchDialog(ShowRebaseBranchDialogMessage m)
        => ShowDialog(new RebaseBranchDialog(
            new RebaseBranchRequest(m.Repo, m.SourceBranch, m.TargetRef, m.TargetDisplay),
            OnDialogClosed));

    private void OnShowPublishBranchDialog(ShowPublishBranchDialogMessage m)
        => ShowDialog(new PublishBranchDialog(
            new PublishBranchRequest(m.Repo, m.LocalBranch),
            OnDialogClosed));

    private void OnShowForcePushDialog(ShowForcePushDialogMessage m)
        => ShowDialog(new ForcePushDialog(m.Repo, m.BranchName, m.Ahead, m.Behind, OnDialogClosed));

    private void OnShowAddSubmoduleDialog(ShowAddSubmoduleDialogMessage m)
        => ShowDialog(new AddSubmoduleDialog(m.Primary, OnDialogClosed));

    private void OnShowUpdateSubmodulesDialog(ShowUpdateSubmodulesDialogMessage m)
        => ShowDialog(new UpdateSubmodulesDialog(m.Primary, m.TargetSubmodule, OnDialogClosed));

    private void OnShowDeinitSubmoduleDialog(ShowDeinitSubmoduleDialogMessage m)
        => ShowDialog(new DeinitSubmoduleDialog(m.Primary, m.Submodule, OnDialogClosed));

    private void ShowDialog(View dialog)
    {
        _dialogSurfaceView.ShowDialog(dialog);
    }

    private void OnDialogClosed()
    {
        _dialogSurfaceView.HideDialog();
    }
}