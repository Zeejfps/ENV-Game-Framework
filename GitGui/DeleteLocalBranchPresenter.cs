using ZGF.Observable;

namespace GitGui;

internal sealed class DeleteLocalBranchPresenter : IDisposable
{
    private readonly IDeleteLocalBranchView _view;
    private readonly DeleteLocalBranchRequest _request;
    private readonly IGitService _gitService;
    private readonly IMessageBus _bus;
    private readonly OperationRunner _runner;

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

        _view.DeleteRequested += OnDeleteRequested;
        _view.DeleteEnabled = true;
    }

    public void Dispose()
    {
        _view.DeleteRequested -= OnDeleteRequested;
    }

    private void OnDeleteRequested()
    {
        if (_runner.IsRunning) return;

        var force = _view.Force;
        var repoId = _request.Repo.Id;

        _view.DeleteEnabled = false;
        _view.ErrorMessage = null;

        _runner.Run(
            () => _gitService.DeleteBranch(_request.Repo, _request.BranchName, force),
            ex => new DeleteBranchOutcome(false, ex.Message),
            outcome =>
            {
                if (!outcome.Success)
                {
                    _view.ErrorMessage = outcome.ErrorMessage ?? "Delete failed.";
                    _view.DeleteEnabled = true;
                    return;
                }
                _view.Close();
                // BranchesViewModel checks against the fresh listing on RefsChangedMessage
                // and drops any selection pointing at a name that no longer exists, so we
                // don't need a separate "branch deleted" signal.
                _bus.Broadcast(new RefsChangedMessage(repoId));
            });
    }
}
