using ZGF.Observable;

namespace GitGui;

internal sealed class RemoveWorktreePresenter : IDisposable
{
    private readonly IRemoveWorktreeView _view;
    private readonly RemoveWorktreeRequest _request;
    private readonly IGitService _gitService;
    private readonly IUiDispatcher _dispatcher;
    private readonly IMessageBus _bus;

    private bool _isRemoving;

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
        _dispatcher = dispatcher;
        _bus = bus;

        _view.RemoveRequested += TryRemove;
        _view.RemoveEnabled = true;
    }

    public void Dispose()
    {
        _view.RemoveRequested -= TryRemove;
    }

    private void TryRemove()
    {
        if (_isRemoving) return;

        _isRemoving = true;
        _view.RemoveEnabled = false;
        _view.ErrorMessage = null;

        var primary = _request.Primary;
        var worktreePath = _request.Worktree.Path;
        var primaryId = primary.Id;
        var force = _view.Force;
        var service = _gitService;
        var dispatcher = _dispatcher;
        var bus = _bus;
        var view = _view;

        Task.Run(() =>
        {
            WorktreeRemoveOutcome outcome;
            try { outcome = service.RemoveWorktree(primary, worktreePath, force); }
            catch (Exception ex) { outcome = new WorktreeRemoveOutcome(false, ex.Message); }

            dispatcher.Post(() =>
            {
                _isRemoving = false;
                if (!outcome.Success)
                {
                    view.ErrorMessage = outcome.ErrorMessage ?? "Remove worktree failed.";
                    view.RemoveEnabled = true;
                    return;
                }
                view.Close();
                bus.Broadcast(new WorktreesChangedMessage(primaryId));
                bus.Broadcast(new RefsChangedMessage(primaryId));
            });
        });
    }
}
