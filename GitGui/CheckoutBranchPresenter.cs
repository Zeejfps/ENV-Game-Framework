using ZGF.Observable;

namespace GitGui;

internal sealed class CheckoutBranchPresenter : IDisposable
{
    private readonly CheckoutRequest _request;
    private readonly ICheckoutBranchView _view;
    private readonly IGitService _gitService;
    private readonly IMessageBus _bus;
    private readonly OperationRunner _runner;

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
        _bus = bus;
        _runner = new OperationRunner(dispatcher);

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
        if (_runner.IsRunning) return;
        _view.CheckoutEnabled = _view.Name.Length > 0;
    }

    private void TryCheckout()
    {
        if (_runner.IsRunning) return;
        var localName = _view.Name;
        if (localName.Length == 0) return;

        var track = _view.Track;
        var repoId = _request.Repo.Id;

        _view.CheckoutEnabled = false;

        _runner.Run(
            () => _gitService.CheckoutRemoteBranch(
                _request.Repo, localName, _request.RemoteName, _request.RemoteBranchName, track),
            ex => new CheckoutOutcome(false, ex.Message),
            outcome =>
            {
                // Close before broadcasting: the error broadcast triggers OverlayView to
                // swap in CheckoutErrorDialog, and a stale view.Close() afterwards would
                // remove the brand-new error dialog instead of this one.
                _view.Close();
                if (outcome.Success)
                    _bus.Broadcast(new RefsChangedMessage(repoId));
                else
                    _bus.Broadcast(new ShowOperationErrorMessage(
                        "Checkout failed",
                        outcome.ErrorMessage ?? "Checkout failed."));
            });
    }
}
