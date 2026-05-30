using ZGF.Observable;

namespace GitGui;

internal sealed class CheckoutBranchDialogViewModel : IDisposable
{
    private readonly SpinnerAnimation _spinner;

    public State<string> Name { get; }
    public State<bool> Track { get; } = new(true);
    public AsyncCommand Checkout { get; }
    public IReadable<bool> CancelEnabled { get; }
    public IReadable<bool> IsBusy { get; }
    public IReadable<float> BusyRotation { get; }

    public event Action? CloseRequested;

    public CheckoutBranchDialogViewModel(
        CheckoutRequest request,
        IGitService gitService,
        IUiDispatcher dispatcher,
        IMessageBus bus)
    {
        _spinner = new SpinnerAnimation(dispatcher);
        IsBusy = _spinner.IsActive;
        BusyRotation = _spinner.Rotation;

        Name = new State<string>(request.SuggestedLocalName);

        var repoId = request.Repo.Id;
        var gate = new Derived<bool>(() => Name.Value.Length > 0);

        Checkout = new AsyncCommand(
            dispatcher,
            work: () =>
            {
                var outcome = gitService.CheckoutRemoteBranch(
                    request.Repo, Name.Value, request.RemoteName, request.RemoteBranchName, Track.Value);
                return outcome.Success ? null : (outcome.ErrorMessage ?? "Checkout failed.");
            },
            // Close before broadcasting: an error broadcast triggers OverlayView to swap in the
            // error dialog, and a stale Close() afterwards would dismiss that brand-new dialog
            // instead of this one. Both paths close, so the ordering holds either way.
            onSuccess: () =>
            {
                CloseRequested?.Invoke();
                bus.Broadcast(new RefsChangedMessage(repoId));
            },
            gate: gate,
            onError: error =>
            {
                CloseRequested?.Invoke();
                bus.Broadcast(new ShowOperationErrorMessage("Checkout failed", error));
            });

        CancelEnabled = new Derived<bool>(() => !Checkout.IsRunning.Value);

        Checkout.IsRunning.Subscribe(running =>
        {
            if (running) _spinner.Start();
            else _spinner.Stop();
        });
    }

    public void Dispose()
    {
        _spinner.Dispose();
    }
}
