using ZGF.Observable;

namespace GitGui;

internal sealed class DeleteRemoteBranchPresenter : IDisposable
{
    private readonly IDeleteRemoteBranchView _view;
    private readonly DeleteRemoteBranchRequest _request;
    private readonly IGitService _gitService;
    private readonly IMessageBus _bus;
    private readonly OperationRunner _runner;

    public DeleteRemoteBranchPresenter(
        IDeleteRemoteBranchView view,
        DeleteRemoteBranchRequest request,
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

        var repoId = _request.Repo.Id;

        _view.DeleteEnabled = false;
        _view.ErrorMessage = null;

        _runner.Run(
            () => _gitService.DeleteRemoteBranch(_request.Repo, _request.RemoteName, _request.BranchName),
            ex => new DeleteRemoteBranchOutcome(false, ex.Message),
            outcome =>
            {
                if (!outcome.Success)
                {
                    _view.ErrorMessage = outcome.ErrorMessage ?? "Delete failed.";
                    _view.DeleteEnabled = true;
                    return;
                }
                _view.Close();
                _bus.Broadcast(new RefsChangedMessage(repoId));
            });
    }
}
