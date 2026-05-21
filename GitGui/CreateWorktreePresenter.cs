using ZGF.Observable;

namespace GitGui;

internal sealed class CreateWorktreePresenter : IDisposable
{
    private readonly ICreateWorktreeView _view;
    private readonly CreateWorktreeRequest _request;
    private readonly IGitService _gitService;
    private readonly IMessageBus _bus;
    private readonly OperationRunner _runner;

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
        _bus = bus;
        _runner = new OperationRunner(dispatcher);

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
        if (_runner.IsRunning) return;
        _view.CreateEnabled = _view.Path.Length > 0 && _view.StartPoint.Length > 0;
    }

    private void TryCreate()
    {
        if (_runner.IsRunning) return;

        var path = _view.Path.Trim();
        var startPoint = _view.StartPoint.Trim();
        if (path.Length == 0 || startPoint.Length == 0) return;

        var newBranch = _view.NewBranchName.Trim();
        var force = _view.Force;
        var primaryId = _request.Primary.Id;

        _view.CreateEnabled = false;
        _view.ErrorMessage = null;

        var req = new WorktreeAddRequest(
            Path: path,
            StartPoint: startPoint,
            NewBranchName: newBranch.Length > 0 ? newBranch : null,
            Force: force);

        _runner.Run(
            () => _gitService.AddWorktree(_request.Primary, req),
            ex => new WorktreeAddOutcome(false, ex.Message),
            outcome =>
            {
                if (!outcome.Success)
                {
                    _view.ErrorMessage = outcome.ErrorMessage ?? "Create worktree failed.";
                    _view.CreateEnabled = _view.Path.Length > 0 && _view.StartPoint.Length > 0;
                    return;
                }
                _view.Close();
                // Trigger registry sync — the watcher will also catch the new .git/worktrees/<name>
                // directory but firing here keeps the UI snappy without waiting for FSW debounce.
                _bus.Broadcast(new WorktreesChangedMessage(primaryId));
                _bus.Broadcast(new RefsChangedMessage(primaryId));
            });
    }
}
