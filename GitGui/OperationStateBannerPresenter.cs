using ZGF.Gui;
using ZGF.Observable;

namespace GitGui;

/// <summary>
/// Owns the <see cref="OperationStateBanner"/>'s state. Lives as a behavior on a
/// long-lived view (AppView) so it can listen for changes the entire app lifetime
/// — the banner itself comes and goes from the tree as state flips between
/// <see cref="RepoOperationState.None"/> and the in-progress states.
/// </summary>
internal sealed class OperationStateBannerPresenter : IViewBehavior, IDisposable
{
    private readonly OperationStateBanner _banner;
    private readonly SubscriptionGroup _subscriptions = new();
    private readonly GenerationGuard _gen = new();

    private IRepoRegistry? _registry;
    private IGitService? _gitService;
    private IUiDispatcher? _dispatcher;
    private IMessageBus? _bus;

    public OperationStateBannerPresenter(OperationStateBanner banner)
    {
        _banner = banner;
        _banner.AbortRequested += OnAbortRequested;
        _banner.ContinueRequested += OnContinueRequested;
    }

    public void AttachToContext(View view, Context context)
    {
        _registry = context.Get<IRepoRegistry>();
        _gitService = context.Get<IGitService>();
        _dispatcher = context.Get<IUiDispatcher>();
        _bus = context.Get<IMessageBus>();
        if (_registry == null || _gitService == null || _dispatcher == null || _bus == null) return;

        _subscriptions.Add(_registry.Active.Subscribe(_ => Reload()));
        _subscriptions.Add(_bus.SubscribeScoped<RefsChangedMessage>(_ => Reload()));
        _subscriptions.Add(_bus.SubscribeScoped<WorkingTreeChangedMessage>(_ => Reload()));
        _subscriptions.Add(_bus.SubscribeScoped<CommitCreatedMessage>(_ => Reload()));
    }

    public void DetachFromContext(View view, Context context)
    {
        Dispose();
    }

    private void OnAbortRequested()
    {
        var repo = _registry?.Active.Value;
        var state = _banner.CurrentState;
        if (repo == null || state == RepoOperationState.None) return;
        _bus?.Broadcast(new ShowAbortOperationDialogMessage(repo, state));
    }

    private void OnContinueRequested()
    {
        var repo = _registry?.Active.Value;
        var state = _banner.CurrentState;
        var service = _gitService;
        var dispatcher = _dispatcher;
        var bus = _bus;
        if (repo == null || state == RepoOperationState.None || service == null
            || dispatcher == null || bus == null) return;

        Task.Run(() =>
        {
            ContinueOperationOutcome outcome;
            try { outcome = service.ContinueOperation(repo, state); }
            catch (Exception ex) { outcome = new ContinueOperationOutcome(false, ex.Message); }

            dispatcher.Post(() =>
            {
                if (outcome.Success)
                {
                    bus.Broadcast(new RefsChangedMessage(repo.Id));
                    bus.Broadcast(new WorkingTreeChangedMessage(repo.Id));
                    return;
                }
                var title = outcome.HasMoreConflicts
                    ? "Resolve remaining conflicts"
                    : "Continue failed";
                bus.Broadcast(new ShowOperationErrorMessage(
                    title,
                    outcome.ErrorMessage ?? "Continue failed."));
            });
        });
    }

    private void Reload()
    {
        var registry = _registry;
        var service = _gitService;
        var dispatcher = _dispatcher;
        if (registry == null || service == null || dispatcher == null) return;

        var repo = registry.Active.Value;
        if (repo == null)
        {
            _gen.Bump();
            _banner.State = RepoOperationState.None;
            return;
        }

        var gen = _gen.Bump();
        var repoId = repo.Id;
        Task.Run(() =>
        {
            RepoOperationState state;
            try { state = service.GetOperationState(repo); }
            catch { state = RepoOperationState.None; }

            dispatcher.Post(() =>
            {
                if (_gen.IsStale(gen)) return;
                if (registry.Active.Value?.Id != repoId) return;
                _banner.State = state;
            });
        });
    }

    public void Dispose()
    {
        _gen.Bump();
        _banner.AbortRequested -= OnAbortRequested;
        _banner.ContinueRequested -= OnContinueRequested;
        _subscriptions.Dispose();
    }
}
