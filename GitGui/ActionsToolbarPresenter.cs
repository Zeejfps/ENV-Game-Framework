using ZGF.Observable;

namespace GitGui;

internal sealed class ActionsToolbarPresenter : IDisposable
{
    // Per-frame angle delta for the loader spinner. Clockwise on screen = negative angle
    // because the orthographic projection has Y up.
    private const int AnimTickMs = 16;
    private const float RotationPerTick = -MathF.Tau * (AnimTickMs / 1000f);

    private readonly IActionsToolbarView _view;
    private readonly IRepoRegistry _registry;
    private readonly IGitService _gitService;
    private readonly IUiDispatcher _dispatcher;
    private readonly IMessageBus _bus;
    private readonly SubscriptionGroup _subscriptions = new();
    private readonly GenerationGuard _statusGen = new();

    private PushStatus _pushStatus = new(null, HasUpstream: false, Ahead: 0, Behind: 0, IsDetached: false);
    private bool _isPushing;
    private bool _isPulling;
    private CancellationTokenSource? _pushAnimCts;
    private CancellationTokenSource? _pullAnimCts;
    private float _pushRotation;
    private float _pullRotation;

    public ActionsToolbarPresenter(
        IActionsToolbarView view,
        IRepoRegistry registry,
        IGitService gitService,
        IUiDispatcher dispatcher,
        IMessageBus bus)
    {
        _view = view;
        _registry = registry;
        _gitService = gitService;
        _dispatcher = dispatcher;
        _bus = bus;

        _view.PushRequested += OnPushRequested;
        _view.PullRequested += OnPullRequested;

        UpdateSyncButtons();

        _subscriptions.Add(_registry.Active.Subscribe(_ => OnRepoOrRefsChanged()));
        _subscriptions.Add(_bus.SubscribeScoped<CommitCreatedMessage>(_ => OnRepoOrRefsChanged()));
        _subscriptions.Add(_bus.SubscribeScoped<RefsChangedMessage>(_ => OnRepoOrRefsChanged()));
    }

    public void Dispose()
    {
        _statusGen.Bump();
        StopPushAnim();
        StopPullAnim();
        _subscriptions.Dispose();
        _view.PushRequested -= OnPushRequested;
        _view.PullRequested -= OnPullRequested;
    }

    private void OnRepoOrRefsChanged()
    {
        _view.Error = null;
        ReloadPushStatus();
    }

    private void ReloadPushStatus()
    {
        var repo = _registry.Active.Value;
        if (repo == null)
        {
            _pushStatus = new PushStatus(null, HasUpstream: false, Ahead: 0, Behind: 0, IsDetached: false);
            UpdateSyncButtons();
            return;
        }

        var gen = _statusGen.Bump();
        var service = _gitService;
        var dispatcher = _dispatcher;

        Task.Run(() =>
        {
            var status = service.GetPushStatus(repo);
            dispatcher.Post(() =>
            {
                if (_statusGen.IsStale(gen)) return;
                if (_registry.Active.Value?.Id != repo.Id) return;
                _pushStatus = status;
                UpdateSyncButtons();
            });
        });
    }

    private void UpdateSyncButtons()
    {
        var hasBranchUpstream = !_pushStatus.IsDetached && _pushStatus.HasUpstream;
        _view.PushEnabled = !_isPushing && hasBranchUpstream && _pushStatus.Ahead > 0;
        _view.PullEnabled = !_isPulling && hasBranchUpstream && _pushStatus.Behind > 0;
    }

    private void OnPushRequested()
    {
        var repo = _registry.Active.Value;
        if (repo == null) return;
        if (_isPushing) return;

        _isPushing = true;
        UpdateSyncButtons();
        _view.Error = null;
        StartPushAnim();

        var service = _gitService;
        var dispatcher = _dispatcher;
        var bus = _bus;

        Task.Run(() =>
        {
            PushOutcome outcome;
            try { outcome = service.Push(repo); }
            catch (Exception ex) { outcome = new PushOutcome(false, ex.Message); }

            dispatcher.Post(() =>
            {
                _isPushing = false;
                StopPushAnim();
                if (!outcome.Success)
                {
                    _view.Error = outcome.ErrorMessage ?? "Push failed.";
                    UpdateSyncButtons();
                    return;
                }

                // Broadcast also re-runs ReloadPushStatus via our own subscription, so we
                // don't call it directly here.
                bus.Broadcast(new RefsChangedMessage(repo.Id));
            });
        });
    }

    private void OnPullRequested()
    {
        var repo = _registry.Active.Value;
        if (repo == null) return;
        if (_isPulling) return;

        _isPulling = true;
        UpdateSyncButtons();
        _view.Error = null;
        StartPullAnim();

        var service = _gitService;
        var dispatcher = _dispatcher;
        var bus = _bus;

        Task.Run(() =>
        {
            PullOutcome outcome;
            try { outcome = service.Pull(repo); }
            catch (Exception ex) { outcome = new PullOutcome(false, ex.Message); }

            dispatcher.Post(() =>
            {
                _isPulling = false;
                StopPullAnim();
                if (!outcome.Success)
                {
                    _view.Error = outcome.ErrorMessage ?? "Pull failed.";
                    UpdateSyncButtons();
                    return;
                }

                bus.Broadcast(new RefsChangedMessage(repo.Id));
            });
        });
    }

    private void StartPushAnim()
    {
        _pushAnimCts?.Cancel();
        _pushAnimCts = new CancellationTokenSource();
        _pushRotation = 0f;
        _view.PushBusy = true;
        RunSpinLoop(_pushAnimCts.Token, isPush: true);
    }

    private void StopPushAnim()
    {
        _pushAnimCts?.Cancel();
        _pushAnimCts?.Dispose();
        _pushAnimCts = null;
        _pushRotation = 0f;
        _view.PushBusy = false;
    }

    private void StartPullAnim()
    {
        _pullAnimCts?.Cancel();
        _pullAnimCts = new CancellationTokenSource();
        _pullRotation = 0f;
        _view.PullBusy = true;
        RunSpinLoop(_pullAnimCts.Token, isPush: false);
    }

    private void StopPullAnim()
    {
        _pullAnimCts?.Cancel();
        _pullAnimCts?.Dispose();
        _pullAnimCts = null;
        _pullRotation = 0f;
        _view.PullBusy = false;
    }

    private void RunSpinLoop(CancellationToken ct, bool isPush)
    {
        var dispatcher = _dispatcher;
        Task.Run(async () =>
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    await Task.Delay(AnimTickMs, ct).ConfigureAwait(false);
                    dispatcher.Post(() =>
                    {
                        if (ct.IsCancellationRequested) return;
                        if (isPush)
                        {
                            _pushRotation += RotationPerTick;
                            _view.PushRotation = _pushRotation;
                        }
                        else
                        {
                            _pullRotation += RotationPerTick;
                            _view.PullRotation = _pullRotation;
                        }
                    });
                }
            }
            catch (OperationCanceledException) { /* expected */ }
        }, ct);
    }
}
