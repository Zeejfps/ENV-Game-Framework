using ZGF.Observable;

namespace GitGui;

internal sealed class CheckoutBranchPresenter : IDisposable
{
    private readonly CheckoutRequest _request;
    private readonly ICheckoutBranchView _view;
    private readonly IGitService _gitService;
    private readonly IUiDispatcher _dispatcher;
    private readonly IMessageBus _bus;

    private bool _isCheckingOut;

    public CheckoutBranchPresenter(
        ICheckoutBranchView view,
        CheckoutRequest request,
        IGitService gitService,
        IUiDispatcher dispatcher,
        IMessageBus bus)
    {
        _view = view;
        _request = request;
        _gitService = gitService;
        _dispatcher = dispatcher;
        _bus = bus;

        _view.NameChanged += OnNameChanged;
        _view.CheckoutRequested += TryCheckout;
        _view.CheckoutEnabled = request.SuggestedLocalName.Length > 0;
        _view.FocusName(request.SuggestedLocalName);
    }

    public void Dispose()
    {
        _view.NameChanged -= OnNameChanged;
        _view.CheckoutRequested -= TryCheckout;
    }

    private void OnNameChanged()
    {
        if (_isCheckingOut) return;
        _view.CheckoutEnabled = _view.Name.Length > 0;
    }

    private void TryCheckout()
    {
        if (_isCheckingOut) return;
        var localName = _view.Name;
        if (localName.Length == 0) return;

        _isCheckingOut = true;
        _view.CheckoutEnabled = false;

        var request = _request;
        var track = _view.Track;
        var service = _gitService;
        var dispatcher = _dispatcher;
        var bus = _bus;
        var view = _view;

        Task.Run(() =>
        {
            CheckoutOutcome outcome;
            try
            {
                outcome = service.CheckoutRemoteBranch(
                    request.Repo, localName, request.RemoteName, request.RemoteBranchName, track);
            }
            catch (Exception ex)
            {
                outcome = new CheckoutOutcome(false, ex.Message);
            }

            dispatcher.Post(() =>
            {
                _isCheckingOut = false;
                // Close before broadcasting: the error broadcast triggers OverlayView to
                // swap in CheckoutErrorDialog, and a stale view.Close() afterwards would
                // remove the brand-new error dialog instead of this one.
                view.Close();
                if (outcome.Success)
                    bus.Broadcast(new RefsChangedMessage(request.Repo.Id));
                else
                    bus.Broadcast(new ShowCheckoutErrorMessage(
                        outcome.ErrorMessage ?? "Checkout failed."));
            });
        });
    }
}