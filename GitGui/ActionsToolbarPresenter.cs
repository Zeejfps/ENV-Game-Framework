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
    private readonly IPlatformShell _shell;
    private readonly IUiDispatcher _dispatcher;
    private readonly IMessageBus _bus;
    private readonly SubscriptionGroup _subscriptions = new();
    private readonly GenerationGuard _statusGen = new();

    private PushStatus _pushStatus = new(null, HasUpstream: false, Ahead: 0, Behind: 0, IsDetached: false);
    private bool _isPushing;
    private bool _isPulling;
    private bool _isFetching;
    private CancellationTokenSource? _pushAnimCts;
    private CancellationTokenSource? _pullAnimCts;
    private CancellationTokenSource? _fetchAnimCts;
    private float _pushRotation;
    private float _pullRotation;
    private float _fetchRotation;

    public ActionsToolbarPresenter(
        IActionsToolbarView view,
        IRepoRegistry registry,
        IGitService gitService,
        IPlatformShell shell,
        IUiDispatcher dispatcher,
        IMessageBus bus)
    {
        _view = view;
        _registry = registry;
        _gitService = gitService;
        _shell = shell;
        _dispatcher = dispatcher;
        _bus = bus;

        _view.PushRequested += OnPushRequested;
        _view.PullRequested += OnPullRequested;
        _view.FetchRequested += OnFetchRequested;
        _view.OpenInFolderRequested += OnOpenInFolderRequested;
        _view.OpenInTerminalRequested += OnOpenInTerminalRequested;
        _view.BranchRequested += OnBranchRequested;

        UpdateSyncButtons();
        UpdateRepoActionButtons();

        _subscriptions.Add(_registry.Active.Subscribe(_ => OnRepoOrRefsChanged()));
        _subscriptions.Add(_bus.SubscribeScoped<CommitCreatedMessage>(_ => OnRepoOrRefsChanged()));
        _subscriptions.Add(_bus.SubscribeScoped<RefsChangedMessage>(_ => OnRepoOrRefsChanged()));
    }

    public void Dispose()
    {
        _statusGen.Bump();
        StopPushAnim();
        StopPullAnim();
        StopFetchAnim();
        _subscriptions.Dispose();
        _view.PushRequested -= OnPushRequested;
        _view.PullRequested -= OnPullRequested;
        _view.FetchRequested -= OnFetchRequested;
        _view.OpenInFolderRequested -= OnOpenInFolderRequested;
        _view.OpenInTerminalRequested -= OnOpenInTerminalRequested;
        _view.BranchRequested -= OnBranchRequested;
    }

    private void OnRepoOrRefsChanged()
    {
        _view.Error = null;
        UpdateRepoActionButtons();
        ReloadPushStatus();
    }

    private void UpdateRepoActionButtons()
    {
        _view.RepoActionsEnabled = _registry.Active.Value != null;
    }

    private void OnOpenInFolderRequested()
    {
        var repo = _registry.Active.Value;
        if (repo == null) return;
        try { _shell.OpenFolder(repo.Path); }
        catch (Exception ex) { _view.Error = $"Open folder failed: {ex.Message}"; }
    }

    private void OnOpenInTerminalRequested()
    {
        var repo = _registry.Active.Value;
        if (repo == null) return;
        try { _shell.OpenTerminal(repo.Path); }
        catch (Exception ex) { _view.Error = $"Open terminal failed: {ex.Message}"; }
    }

    private void OnBranchRequested()
    {
        var repo = _registry.Active.Value;
        if (repo == null) return;
        // Suggest the current branch as the starting point (matches Fork's default). When HEAD
        // is detached, fall back to "HEAD" so the dialog still has a meaningful prefill.
        var suggested = _pushStatus.IsDetached || string.IsNullOrEmpty(_pushStatus.CurrentBranchName)
            ? "HEAD"
            : _pushStatus.CurrentBranchName;
        _bus.Broadcast(new ShowCreateBranchDialogMessage(repo, suggested));
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
        _view.FetchEnabled = !_isFetching && _registry.Active.Value != null;
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

    private void OnFetchRequested()
    {
        var repo = _registry.Active.Value;
        if (repo == null) return;
        if (_isFetching) return;

        _isFetching = true;
        UpdateSyncButtons();
        _view.Error = null;
        StartFetchAnim();

        var service = _gitService;
        var dispatcher = _dispatcher;
        var bus = _bus;

        Task.Run(() =>
        {
            FetchOutcome outcome;
            try { outcome = service.Fetch(repo); }
            catch (Exception ex) { outcome = new FetchOutcome(false, ex.Message); }

            dispatcher.Post(() =>
            {
                _isFetching = false;
                StopFetchAnim();
                if (!outcome.Success)
                {
                    _view.Error = outcome.ErrorMessage ?? "Fetch failed.";
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
        RunSpinLoop(_pushAnimCts.Token, SpinTarget.Push);
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
        RunSpinLoop(_pullAnimCts.Token, SpinTarget.Pull);
    }

    private void StopPullAnim()
    {
        _pullAnimCts?.Cancel();
        _pullAnimCts?.Dispose();
        _pullAnimCts = null;
        _pullRotation = 0f;
        _view.PullBusy = false;
    }

    private void StartFetchAnim()
    {
        _fetchAnimCts?.Cancel();
        _fetchAnimCts = new CancellationTokenSource();
        _fetchRotation = 0f;
        _view.FetchBusy = true;
        RunSpinLoop(_fetchAnimCts.Token, SpinTarget.Fetch);
    }

    private void StopFetchAnim()
    {
        _fetchAnimCts?.Cancel();
        _fetchAnimCts?.Dispose();
        _fetchAnimCts = null;
        _fetchRotation = 0f;
        _view.FetchBusy = false;
    }

    private enum SpinTarget { Push, Pull, Fetch }

    private void RunSpinLoop(CancellationToken ct, SpinTarget target)
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
                        switch (target)
                        {
                            case SpinTarget.Push:
                                _pushRotation += RotationPerTick;
                                _view.PushRotation = _pushRotation;
                                break;
                            case SpinTarget.Pull:
                                _pullRotation += RotationPerTick;
                                _view.PullRotation = _pullRotation;
                                break;
                            case SpinTarget.Fetch:
                                _fetchRotation += RotationPerTick;
                                _view.FetchRotation = _fetchRotation;
                                break;
                        }
                    });
                }
            }
            catch (OperationCanceledException) { /* expected */ }
        }, ct);
    }
}
