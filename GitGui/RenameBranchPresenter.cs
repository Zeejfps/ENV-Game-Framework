using ZGF.Observable;

namespace GitGui;

internal sealed class RenameBranchPresenter : IDisposable
{
    private readonly IRenameBranchView _view;
    private readonly RenameBranchRequest _request;
    private readonly IGitService _gitService;
    private readonly IMessageBus _bus;
    private readonly OperationRunner _runner;

    public RenameBranchPresenter(
        IRenameBranchView view,
        RenameBranchRequest request,
        IGitService gitService,
        IUiDispatcher dispatcher,
        IMessageBus bus)
    {
        _view = view;
        _request = request;
        _gitService = gitService;
        _bus = bus;
        _runner = new OperationRunner(dispatcher);

        _view.NameChanged += OnNameChanged;
        _view.RenameRequested += TryRename;
        _view.RenameEnabled = false;
        _view.FocusName();
    }

    public void Dispose()
    {
        _view.NameChanged -= OnNameChanged;
        _view.RenameRequested -= TryRename;
    }

    private void OnNameChanged()
    {
        if (_runner.IsRunning) return;
        var name = _view.Name;
        _view.RenameEnabled = name.Length > 0 && name != _request.CurrentName;
    }

    private void TryRename()
    {
        if (_runner.IsRunning) return;
        var newName = _view.Name;
        if (newName.Length == 0 || newName == _request.CurrentName) return;

        var force = _view.Force;
        var repoId = _request.Repo.Id;
        var oldName = _request.CurrentName;

        _view.RenameEnabled = false;
        _view.ErrorMessage = null;

        _runner.Run(
            () => _gitService.RenameBranch(_request.Repo, oldName, newName, force),
            ex => new RenameBranchOutcome(false, ex.Message),
            outcome =>
            {
                if (!outcome.Success)
                {
                    _view.ErrorMessage = outcome.ErrorMessage ?? "Rename failed.";
                    _view.RenameEnabled = _view.Name.Length > 0 && _view.Name != oldName;
                    return;
                }
                _view.Close();
                _bus.Broadcast(new RefsChangedMessage(repoId));
            });
    }
}
