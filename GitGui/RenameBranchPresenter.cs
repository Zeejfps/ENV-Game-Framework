using ZGF.Observable;

namespace GitGui;

internal sealed class RenameBranchPresenter : IDisposable
{
    private readonly IRenameBranchView _view;
    private readonly RenameBranchRequest _request;
    private readonly IGitService _gitService;
    private readonly IUiDispatcher _dispatcher;
    private readonly IMessageBus _bus;

    private bool _isRenaming;

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
        _dispatcher = dispatcher;
        _bus = bus;

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
        if (_isRenaming) return;
        var name = _view.Name;
        _view.RenameEnabled = name.Length > 0 && name != _request.CurrentName;
    }

    private void TryRename()
    {
        if (_isRenaming) return;
        var newName = _view.Name;
        if (newName.Length == 0 || newName == _request.CurrentName) return;

        var force = _view.Force;

        _isRenaming = true;
        _view.RenameEnabled = false;
        _view.ErrorMessage = null;

        var repo = _request.Repo;
        var oldName = _request.CurrentName;
        var service = _gitService;
        var dispatcher = _dispatcher;
        var bus = _bus;
        var view = _view;

        Task.Run(() =>
        {
            RenameBranchOutcome outcome;
            try
            {
                outcome = service.RenameBranch(repo, oldName, newName, force);
            }
            catch (Exception ex)
            {
                outcome = new RenameBranchOutcome(false, ex.Message);
            }

            dispatcher.Post(() =>
            {
                _isRenaming = false;
                if (!outcome.Success)
                {
                    view.ErrorMessage = outcome.ErrorMessage ?? "Rename failed.";
                    view.RenameEnabled = view.Name.Length > 0 && view.Name != oldName;
                    return;
                }
                view.Close();
                bus.Broadcast(new RefsChangedMessage(repo.Id));
            });
        });
    }
}
