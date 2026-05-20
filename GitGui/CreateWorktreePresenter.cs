using ZGF.Observable;

namespace GitGui;

internal sealed class CreateWorktreePresenter : IDisposable
{
    private readonly ICreateWorktreeView _view;
    private readonly CreateWorktreeRequest _request;
    private readonly IGitService _gitService;
    private readonly IUiDispatcher _dispatcher;
    private readonly IMessageBus _bus;

    private bool _isCreating;

    public CreateWorktreePresenter(
        ICreateWorktreeView view,
        CreateWorktreeRequest request,
        IGitService gitService,
        IUiDispatcher dispatcher,
        IMessageBus bus)
    {
        _view = view;
        _request = request;
        _gitService = gitService;
        _dispatcher = dispatcher;
        _bus = bus;

        _view.InputsChanged += OnInputsChanged;
        _view.CreateRequested += TryCreate;
        _view.CreateEnabled = false;
        _view.FocusPath();
    }

    public void Dispose()
    {
        _view.InputsChanged -= OnInputsChanged;
        _view.CreateRequested -= TryCreate;
    }

    private void OnInputsChanged()
    {
        if (_isCreating) return;
        _view.CreateEnabled = _view.Path.Length > 0 && _view.StartPoint.Length > 0;
    }

    private void TryCreate()
    {
        if (_isCreating) return;

        var path = _view.Path.Trim();
        var startPoint = _view.StartPoint.Trim();
        if (path.Length == 0 || startPoint.Length == 0) return;

        var newBranch = _view.NewBranchName.Trim();
        var force = _view.Force;

        _isCreating = true;
        _view.CreateEnabled = false;
        _view.ErrorMessage = null;

        var primary = _request.Primary;
        var primaryId = primary.Id;
        var service = _gitService;
        var dispatcher = _dispatcher;
        var bus = _bus;
        var view = _view;

        var req = new WorktreeAddRequest(
            Path: path,
            StartPoint: startPoint,
            NewBranchName: newBranch.Length > 0 ? newBranch : null,
            Force: force);

        Task.Run(() =>
        {
            WorktreeAddOutcome outcome;
            try { outcome = service.AddWorktree(primary, req); }
            catch (Exception ex) { outcome = new WorktreeAddOutcome(false, ex.Message); }

            dispatcher.Post(() =>
            {
                _isCreating = false;
                if (!outcome.Success)
                {
                    view.ErrorMessage = outcome.ErrorMessage ?? "Create worktree failed.";
                    view.CreateEnabled = view.Path.Length > 0 && view.StartPoint.Length > 0;
                    return;
                }
                view.Close();
                // Trigger registry sync — the watcher will also catch the new .git/worktrees/<name>
                // directory but firing here keeps the UI snappy without waiting for FSW debounce.
                bus.Broadcast(new WorktreesChangedMessage(primaryId));
                bus.Broadcast(new RefsChangedMessage(primaryId));
            });
        });
    }
}
