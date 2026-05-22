using ZGF.Observable;

namespace GitGui;

internal sealed class DeleteLocalBranchPresenter : IDisposable
{
    private readonly IDeleteLocalBranchView _view;
    private readonly DeleteLocalBranchRequest _request;
    private readonly IGitService _gitService;
    private readonly IMessageBus _bus;
    private readonly OperationRunner _runner;
    private readonly SpinnerAnimation _spinner;
    private readonly SubscriptionGroup _subscriptions = new();

    public DeleteLocalBranchPresenter(
        IDeleteLocalBranchView view,
        DeleteLocalBranchRequest request,
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

        _view.DeleteRequested += OnDeleteRequested;
        _view.DeleteEnabled = true;
        _view.CancelEnabled = true;
    }

    public void Dispose()
    {
        _subscriptions.Dispose();
        _spinner.Dispose();
        _view.DeleteRequested -= OnDeleteRequested;
    }

    private void OnDeleteRequested()
    {
        if (_runner.IsRunning) return;

        var force = _view.Force;
        var deleteRemote = _view.DeleteRemote
            && !string.IsNullOrEmpty(_request.UpstreamRemote)
            && !string.IsNullOrEmpty(_request.UpstreamBranch);
        var repoId = _request.Repo.Id;
        var remoteName = _request.UpstreamRemote;
        var remoteBranch = _request.UpstreamBranch;

        _view.DeleteEnabled = false;
        _view.CancelEnabled = false;
        _view.ErrorMessage = null;
        _spinner.Start();

        _runner.Run(
            () =>
            {
                var local = _gitService.DeleteBranch(_request.Repo, _request.BranchName, force);
                if (!local.Success || !deleteRemote)
                    return new CombinedOutcome(local, RemoteAttempted: false, RemoteOutcome: null);

                DeleteRemoteBranchOutcome remote;
                try { remote = _gitService.DeleteRemoteBranch(_request.Repo, remoteName!, remoteBranch!); }
                catch (Exception ex) { remote = new DeleteRemoteBranchOutcome(false, ex.Message); }
                return new CombinedOutcome(local, RemoteAttempted: true, RemoteOutcome: remote);
            },
            ex => new CombinedOutcome(new DeleteBranchOutcome(false, ex.Message), RemoteAttempted: false, RemoteOutcome: null),
            combined =>
            {
                _spinner.Stop();

                if (!combined.Local.Success)
                {
                    _view.ErrorMessage = combined.Local.ErrorMessage ?? "Delete failed.";
                    _view.DeleteEnabled = true;
                    _view.CancelEnabled = true;
                    return;
                }

                _view.Close();
                _bus.Broadcast(new RefsChangedMessage(repoId));

                if (combined.RemoteAttempted && combined.RemoteOutcome is { Success: false } failed)
                {
                    _bus.Broadcast(new ShowOperationErrorMessage(
                        "Remote delete failed",
                        failed.ErrorMessage
                            ?? $"Local branch deleted, but failed to delete '{remoteBranch}' on '{remoteName}'."));
                }
            });
    }

    private readonly record struct CombinedOutcome(
        DeleteBranchOutcome Local,
        bool RemoteAttempted,
        DeleteRemoteBranchOutcome? RemoteOutcome);
}
