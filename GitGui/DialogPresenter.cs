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
        _messageBus?.Subscribe<ShowStashDialogMessage>(OnShowStashDialog);
        _messageBus?.Subscribe<ShowDropStashDialogMessage>(OnShowDropStashDialog);
        _messageBus?.Subscribe<ShowAbortOperationDialogMessage>(OnShowAbortOperationDialog);
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

    private void OnShowStashDialog(ShowStashDialogMessage m)
        => ShowDialog(new StashDialog(m.Repo, OnDialogClosed));

    private void OnShowDropStashDialog(ShowDropStashDialogMessage m)
        => ShowDialog(new DropStashDialog(m.Repo, m.Index, m.Label, m.Subject, OnDialogClosed));

    private void OnShowAbortOperationDialog(ShowAbortOperationDialogMessage m)
        => ShowDialog(new AbortOperationDialog(m.Repo, m.State, OnDialogClosed));

    private void ShowDialog(View dialog)
    {
        _dialogSurfaceView.ShowDialog(dialog);
    }

    private void OnDialogClosed()
    {
        _dialogSurfaceView.HideDialog();
    }
}