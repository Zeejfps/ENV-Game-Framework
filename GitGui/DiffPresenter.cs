using ZGF.Observable;

namespace GitGui;

internal sealed class DiffPresenter : IDisposable
{
    private const string EmptyPlaceholder = "Select a file to view diff.";
    private const string LoadingPlaceholder = "Loading…";

    private readonly IDiffView _view;
    private readonly IRepoRegistry _registry;
    private readonly IGitService _gitService;
    private readonly IUiDispatcher _dispatcher;
    private readonly SubscriptionGroup _subscriptions = new();
    private readonly GenerationGuard _loadGen = new();

    public DiffPresenter(
        IDiffView view,
        IRepoRegistry registry,
        IGitService gitService,
        IUiDispatcher dispatcher)
    {
        _view = view;
        _registry = registry;
        _gitService = gitService;
        _dispatcher = dispatcher;

        // Subscribe fires immediately with the current target, so any SetTarget calls
        // made before attach kick off a load as soon as services arrive.
        _subscriptions.Add(_view.Target.Subscribe(_ => StartLoad()));
    }

    public void Dispose()
    {
        _loadGen.Bump();
        _subscriptions.Dispose();
    }

    private void StartLoad()
    {
        var gen = _loadGen.Bump();
        var target = _view.Target.Value;

        if (target == null)
        {
            _view.SetViewModel(new DiffViewModel.Placeholder(EmptyPlaceholder));
            return;
        }

        var repo = _registry.Active.Value;
        if (repo == null) return;

        _view.SetViewModel(new DiffViewModel.Placeholder(LoadingPlaceholder));

        var service = _gitService;
        var dispatcher = _dispatcher;
        var path = target.Path;
        var side = target.Side;
        Task.Run(() =>
        {
            DiffViewModel result;
            try
            {
                var diff = service.GetDiff(repo, path, side);
                result = new DiffViewModel.Loaded(diff);
            }
            catch (Exception ex)
            {
                result = new DiffViewModel.Placeholder(ex.Message);
            }

            dispatcher.Post(() =>
            {
                if (_loadGen.IsStale(gen)) return;
                _view.SetViewModel(result);
            });
        });
    }
}
