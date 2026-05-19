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
        _messageBus?.Subscribe<ShowCheckoutErrorMessage>(OnShowCheckoutError);
        _messageBus?.Subscribe<ShowDiscardChangesDialogMessage>(OnShowDiscardChangesDialog);
        _messageBus?.Subscribe<ShowCreateBranchDialogMessage>(OnShowCreateBranchDialog);
    }

    public void DetachFromContext(View view, Context context)
    {
    }

    private void OnAddRepoMessageReceived(AddRepoMessage _)
        => ShowDialog(new AddRepoDialog(OnDialogClosed));

    private void OnShowCheckoutDialog(ShowCheckoutDialogMessage m)
        => ShowDialog(
            new CheckoutBranchDialog(m.Repo, m.RemoteName, m.RemoteBranchName, m.SuggestedLocalName, OnDialogClosed));

    private void OnShowCheckoutError(ShowCheckoutErrorMessage m)
        => ShowDialog(new CheckoutErrorDialog(m.Message, OnDialogClosed));

    private void OnShowDiscardChangesDialog(ShowDiscardChangesDialogMessage m)
        => ShowDialog(new DiscardChangesDialog(m.Repo, m.Paths, OnDialogClosed));

    private void OnShowCreateBranchDialog(ShowCreateBranchDialogMessage m)
        => ShowDialog(new CreateBranchDialog(m.Repo, m.SuggestedStartPoint, OnDialogClosed));

    private void ShowDialog(View dialog)
    {
        _dialogSurfaceView.ShowDialog(dialog);
    }

    private void OnDialogClosed()
    {
        _dialogSurfaceView.HideDialog();
    }
}