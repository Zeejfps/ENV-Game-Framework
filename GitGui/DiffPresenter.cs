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

    // Tracks the most recently pushed view model so we can decide whether to show
    // "Loading…" when a new target arrives. Transitioning between two Loaded diffs
    // (e.g. stage flips fileA from Unstaged to Staged) leaves the previous content
    // visible while the new diff fetches — for sub-100ms libgit2 reads the swap is
    // imperceptible, and the placeholder flash isn't worth the visual noise.
    private DiffViewModel? _lastPushed;

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
            Push(new DiffViewModel.Placeholder(EmptyPlaceholder));
            return;
        }

        var repo = _registry.Active.Value;
        if (repo == null) return;

        // Only show "Loading…" when there's nothing useful on screen to keep. If we're
        // currently showing a Loaded diff, leave it visible while the new one fetches —
        // stage/unstage of the same path resolves in milliseconds and the flash of a
        // placeholder between two near-identical diffs is more jarring than the brief
        // moment of stale content.
        if (_lastPushed is not DiffViewModel.Loaded)
            Push(new DiffViewModel.Placeholder(LoadingPlaceholder));

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
                Push(result);
            });
        });
    }

    private void Push(DiffViewModel vm)
    {
        _lastPushed = vm;
        _view.SetViewModel(vm);
    }
}
