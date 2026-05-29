using ZGF.Observable;

namespace GitGui;

internal sealed class CheckoutBranchDialogViewModel : IDisposable
{
    private readonly CheckoutRequest _request;
    private readonly IGitService _gitService;
    private readonly IMessageBus _bus;
    private readonly OperationRunner _runner;
    private readonly State<bool> _isRunning = new(false);

    public State<string> Name { get; }
    public State<bool> Track { get; } = new(true);
    public Command Checkout { get; }

    public event Action? CloseRequested;

    public CheckoutBranchDialogViewModel(
        CheckoutRequest request,
        IGitService gitService,
        IUiDispatcher dispatcher,
        IMessageBus bus)
    {
        _request = request;
        _gitService = gitService;
        _bus = bus;
        _runner = new OperationRunner(dispatcher);

        Name = new State<string>(request.SuggestedLocalName);

        var canCheckout = new Derived<bool>(() => !_isRunning.Value && Name.Value.Length > 0);
        Checkout = new Command(DoCheckout, canCheckout);
    }

    private void DoCheckout()
    {
        if (_isRunning.Value) return;
        var name = Name.Value;
        var track = Track.Value;
        var repoId = _request.Repo.Id;
        _isRunning.Value = true;

        _runner.Run(
            () => _gitService.CheckoutRemoteBranch(
                _request.Repo, name, _request.RemoteName, _request.RemoteBranchName, track),
            ex => new CheckoutOutcome(false, ex.Message),
            outcome =>
            {
                _isRunning.Value = false;
                // Close before broadcasting the error: the error broadcast triggers
                // OverlayView to swap in the error dialog, and a stale Close() afterwards
                // would dismiss that brand-new dialog instead of this one.
                CloseRequested?.Invoke();
                if (outcome.Success)
                    _bus.Broadcast(new RefsChangedMessage(repoId));
                else
                    _bus.Broadcast(new ShowOperationErrorMessage(
                        "Checkout failed",
                        outcome.ErrorMessage ?? "Checkout failed."));
            });
    }

    public void Dispose() { }
}
