using ZGF.Observable;

namespace GitGui;

internal sealed class CommitDetailsPresenter : IDisposable
{
    private const string DefaultPlaceholder = "Select a commit to view details.";

    private readonly ICommitDetailsView _view;
    private readonly IGitService _gitService;
    private readonly IRepoRegistry _registry;
    private readonly IUiDispatcher _dispatcher;
    private readonly IMessageBus _bus;
    private readonly SubscriptionGroup _subscriptions = new();
    private readonly GenerationGuard _loadGen = new();

    public CommitDetailsPresenter(
        ICommitDetailsView view,
        IGitService gitService,
        IRepoRegistry registry,
        IUiDispatcher dispatcher,
        IMessageBus bus)
    {
        _view = view;
        _gitService = gitService;
        _registry = registry;
        _dispatcher = dispatcher;
        _bus = bus;

        _subscriptions.Add(_bus.SubscribeScoped<CommitSelectedMessage>(OnCommitSelected));
        _view.ShowPlaceholder(DefaultPlaceholder);
    }

    public void Dispose()
    {
        _loadGen.Bump();
        _subscriptions.Dispose();
    }

    private void OnCommitSelected(CommitSelectedMessage msg)
    {
        if (string.IsNullOrEmpty(msg.Sha))
        {
            _loadGen.Bump();
            _view.ShowPlaceholder(DefaultPlaceholder);
            return;
        }
        StartLoad(msg.RepoId, msg.Sha);
    }

    private void StartLoad(Guid repoId, string sha)
    {
        var repo = _registry.Active.Value;
        if (repo == null || repo.Id != repoId) return;

        var gen = _loadGen.Bump();
        _view.ShowPlaceholder("Loading…");

        var service = _gitService;
        var dispatcher = _dispatcher;
        var view = _view;

        Task.Run(() =>
        {
            string? error = null;
            CommitDetails? details = null;
            try
            {
                details = service.LoadDetails(repo, sha);
                if (details.ErrorMessage != null)
                {
                    error = details.ErrorMessage;
                    details = null;
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }

            dispatcher.Post(() =>
            {
                if (_loadGen.IsStale(gen)) return;
                if (error != null) view.ShowPlaceholder(error);
                else if (details != null) view.ShowDetails(details);
            });
        });
    }
}
