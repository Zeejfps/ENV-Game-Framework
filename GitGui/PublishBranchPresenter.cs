using ZGF.Observable;

namespace GitGui;

internal sealed class PublishBranchPresenter : IDisposable
{
    private readonly IPublishBranchView _view;
    private readonly PublishBranchRequest _request;
    private readonly IGitService _gitService;
    private readonly IUiDispatcher _dispatcher;
    private readonly IMessageBus _bus;
    private readonly OperationRunner _runner;

    public PublishBranchPresenter(
        IPublishBranchView view,
        PublishBranchRequest request,
        IGitService gitService,
        IUiDispatcher dispatcher,
        IMessageBus bus)
    {
        _view = view;
        _request = request;
        _gitService = gitService;
        _dispatcher = dispatcher;
        _bus = bus;
        _runner = new OperationRunner(dispatcher);

        _view.PublishRequested += TryPublish;
        _view.PublishEnabled = false;

        LoadRemotes();
    }

    public void Dispose()
    {
        _view.PublishRequested -= TryPublish;
    }

    private void LoadRemotes()
    {
        var repo = _request.Repo;
        var service = _gitService;
        var dispatcher = _dispatcher;
        var view = _view;

        Task.Run(() =>
        {
            IReadOnlyList<string> remotes;
            try { remotes = service.GetRemoteNames(repo); }
            catch { remotes = Array.Empty<string>(); }

            dispatcher.Post(() =>
            {
                view.SetRemotes(remotes);
                if (remotes.Count == 0)
                {
                    view.ErrorMessage = "No remotes configured. Add one with: git remote add origin <url>";
                    view.PublishEnabled = false;
                }
                else
                {
                    view.PublishEnabled = true;
                }
            });
        });
    }

    private void TryPublish()
    {
        if (_runner.IsRunning) return;
        var remote = _view.SelectedRemote;
        if (string.IsNullOrEmpty(remote)) return;

        var setUpstream = _view.SetUpstream;
        var local = _request.LocalBranch;
        var repoId = _request.Repo.Id;

        _view.PublishEnabled = false;
        _view.ErrorMessage = null;

        _runner.Run(
            () => _gitService.PublishBranch(_request.Repo, local, remote, local, setUpstream),
            ex => new PushOutcome(false, ex.Message),
            outcome =>
            {
                if (!outcome.Success)
                {
                    _view.ErrorMessage = outcome.ErrorMessage ?? "Publish failed.";
                    _view.PublishEnabled = true;
                    return;
                }
                _view.Close();
                _bus.Broadcast(new RefsChangedMessage(repoId));
            });
    }
}
