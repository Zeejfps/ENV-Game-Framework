using ZGF.Gui;
using ZGF.Gui.Views;

namespace GitGui;

public sealed class DialogPresenter : IViewBehavior
{
    private readonly AppView _view;
    private readonly MultiChildView _overlay;

    public DialogPresenter(AppView view)
    {
        _view = view;
        
        _overlay = new RectView
        {
            BackgroundColor = 0xB0000000,
            ZIndex = 1000,
        };
    }

    public void AttachToContext(View view, Context context)
    {
        var _messageBus = context.Get<IMessageBus>();
        _messageBus?.Subscribe<AddRepoMessage>(OnAddRepoMessageReceived);
        _messageBus?.Subscribe<ShowCheckoutDialogMessage>(OnShowCheckoutDialog);
        _messageBus?.Subscribe<ShowCheckoutErrorMessage>(OnShowCheckoutError);
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

    private void ShowDialog(View dialog)
    {
        _view.Children.Add(_overlay);
        _overlay.Children.Add(new CenterView
        {
            Children =
            {
                dialog,
            }
        });
    }

    private void OnDialogClosed()
    {
        _overlay.Children.Clear();
        _view.Children.Remove(_overlay);
    }
}