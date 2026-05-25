using ZGF.Gui;
using ZGF.Observable;

namespace GitGui;

internal sealed class AbortOperationPresenter : IDisposable
{
    private readonly IAbortOperationView _view;
    private readonly AbortOperationRequest _request;
    private readonly IGitService _gitService;
    private readonly IMessageBus _bus;
    private readonly SpinnerAnimation _spinner;
    private readonly SubscriptionGroup _subscriptions = new();
    private readonly OperationRunner _runner;

    // Set after the regular --abort returned with ForceQuitAvailable=true. The next click
    // sends forceQuit=true, which runs `git X --quit` (or removes sentinels directly) to
    // clear the stuck state without trying to restore HEAD.
    private bool _forceQuitMode;

    public AbortOperationPresenter(
        IAbortOperationView view,
        AbortOperationRequest request,
        IGitService gitService,
        IUiDispatcher dispatcher,
        IMessageBus bus)
    {
        _view = view;
        _request = request;
        _gitService = gitService;
        _bus = bus;
        _runner = new OperationRunner(dispatcher);

        _spinner = new SpinnerAnimation(dispatcher);
        _subscriptions.Add(_spinner.IsActive.Subscribe(b => _view.IsBusy = b));
        _subscriptions.Add(_spinner.Rotation.Subscribe(r => _view.BusyRotation = r));

        _view.AbortRequested += OnAbortRequested;
        _view.AbortEnabled = request.State != RepoOperationState.None;
    }

    public void Dispose()
    {
        _subscriptions.Dispose();
        _spinner.Dispose();
        _view.AbortRequested -= OnAbortRequested;
    }

    private void OnAbortRequested()
    {
        if (_runner.IsRunning) return;
        if (_request.State == RepoOperationState.None) return;

        var forceQuit = _forceQuitMode;
        var repoId = _request.Repo.Id;

        _view.AbortEnabled = false;
        _view.CancelEnabled = false;
        _view.ErrorMessage = null;
        _spinner.Start();

        _runner.Run(
            () => _gitService.AbortOperation(_request.Repo, _request.State, forceQuit),
            ex => new AbortOperationOutcome(false, ex.Message),
            outcome =>
            {
                _spinner.Stop();
                if (!outcome.Success)
                {
                    _view.ErrorMessage = outcome.ErrorMessage ?? "Abort failed.";
                    _view.AbortEnabled = true;
                    _view.CancelEnabled = true;
                    // If git couldn't recover (malformed sentinel dir, etc.) but the state
                    // is force-clearable, flip the confirm button so the next click is the
                    // explicit "give up restoring HEAD" path. Only flip once — once we're
                    // in force-quit mode, repeated failures keep the same button label so
                    // the user isn't ping-ponged.
                    if (outcome.ForceQuitAvailable && !_forceQuitMode)
                    {
                        _forceQuitMode = true;
                        _view.ConfirmButtonLabel = "Force clear";
                    }
                    return;
                }
                _view.Close();
                // Both messages: RefsChangedMessage refreshes the commit graph and branch
                // ahead/behind; WorkingTreeChangedMessage refreshes the local-changes panels
                // (where the conflict markers and ! badges live).
                _bus.Broadcast(new RefsChangedMessage(repoId));
                _bus.Broadcast(new WorkingTreeChangedMessage(repoId));
            });
    }
}
