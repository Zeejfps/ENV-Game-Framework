using ZGF.Observable;

namespace GitGui;

internal sealed class PublishBranchPresenter : IDisposable
{
    private readonly IPublishBranchView _view;
    private readonly PublishBranchRequest _request;
    private readonly IGitService _gitService;
    private readonly IUiDispatcher _dispatcher;
    private readonly IMessageBus _bus;

    private bool _isPublishing;

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
        if (_isPublishing) return;
        var remote = _view.SelectedRemote;
        if (string.IsNullOrEmpty(remote)) return;

        _isPublishing = true;
        _view.PublishEnabled = false;
        _view.ErrorMessage = null;

        var repo = _request.Repo;
        var local = _request.LocalBranch;
        var setUpstream = _view.SetUpstream;
        var service = _gitService;
        var dispatcher = _dispatcher;
        var bus = _bus;
        var view = _view;

        Task.Run(() =>
        {
            PushOutcome outcome;
            try { outcome = service.PublishBranch(repo, local, remote, local, setUpstream); }
            catch (Exception ex) { outcome = new PushOutcome(false, ex.Message); }

            dispatcher.Post(() =>
            {
                _isPublishing = false;
                if (!outcome.Success)
                {
                    view.ErrorMessage = outcome.ErrorMessage ?? "Publish failed.";
                    view.PublishEnabled = true;
                    return;
                }
                view.Close();
                bus.Broadcast(new RefsChangedMessage(repo.Id));
            });
        });
    }
}
