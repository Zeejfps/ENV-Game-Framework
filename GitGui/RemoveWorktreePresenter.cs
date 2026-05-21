using ZGF.Observable;

namespace GitGui;

internal sealed class RemoveWorktreePresenter : IDisposable
{
    private readonly IRemoveWorktreeView _view;
    private readonly RemoveWorktreeRequest _request;
    private readonly IGitService _gitService;
    private readonly IMessageBus _bus;
    private readonly OperationRunner _runner;

    public RemoveWorktreePresenter(
        IRemoveWorktreeView view,
        RemoveWorktreeRequest request,
        IGitService gitService,
        IUiDispatcher dispatcher,
        IMessageBus bus)
    {
        _view = view;
        _request = request;
        _gitService = gitService;
        _bus = bus;
        _runner = new OperationRunner(dispatcher);

        _view.RemoveRequested += TryRemove;
        _view.RemoveEnabled = true;
    }

    public void Dispose()
    {
        _view.RemoveRequested -= TryRemove;
    }

    private void TryRemove()
    {
        if (_runner.IsRunning) return;

        var worktreePath = _request.Worktree.Path;
        var primaryId = _request.Primary.Id;
        var force = _view.Force;

        _view.RemoveEnabled = false;
        _view.ErrorMessage = null;

        _runner.Run(
            () => _gitService.RemoveWorktree(_request.Primary, worktreePath, force),
            ex => new WorktreeRemoveOutcome(false, ex.Message),
            outcome =>
            {
                if (!outcome.Success)
                {
                    _view.ErrorMessage = outcome.ErrorMessage ?? "Remove worktree failed.";
                    _view.RemoveEnabled = true;
                    return;
                }
                _view.Close();
                _bus.Broadcast(new WorktreesChangedMessage(primaryId));
                _bus.Broadcast(new RefsChangedMessage(primaryId));
            });
    }
}
