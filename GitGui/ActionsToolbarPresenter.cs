using ZGF.Observable;

namespace GitGui;

internal sealed class ActionsToolbarPresenter : IDisposable
{
    private readonly IActionsToolbarView _view;
    private readonly IRepoRegistry _registry;
    private readonly IGitService _gitService;
    private readonly IPlatformShell _shell;
    private readonly IUiDispatcher _dispatcher;
    private readonly IMessageBus _bus;
    private readonly SubscriptionGroup _subscriptions = new();
    private readonly GenerationGuard _statusGen = new();

    private readonly SpinnerAnimation _pushSpinner;
    private readonly SpinnerAnimation _pullSpinner;
    private readonly SpinnerAnimation _fetchSpinner;

    private PushStatus _pushStatus = new(null, HasUpstream: false, Ahead: 0, Behind: 0, IsDetached: false);
    private bool _isPushing;
    private bool _isPulling;
    private bool _isFetching;

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

        _pushSpinner = new SpinnerAnimation(dispatcher);
        _pullSpinner = new SpinnerAnimation(dispatcher);
        _fetchSpinner = new SpinnerAnimation(dispatcher);

        _pushSpinner.IsActive.Subscribe(b => _view.PushBusy = b);
        _pushSpinner.Rotation.Subscribe(r => _view.PushRotation = r);
        _pullSpinner.IsActive.Subscribe(b => _view.PullBusy = b);
        _pullSpinner.Rotation.Subscribe(r => _view.PullRotation = r);
        _fetchSpinner.IsActive.Subscribe(b => _view.FetchBusy = b);
        _fetchSpinner.Rotation.Subscribe(r => _view.FetchRotation = r);

        _view.PushRequested += OnPushRequested;
        _view.PullRequested += OnPullRequested;
        _view.FetchRequested += OnFetchRequested;
        _view.OpenInFolderRequested += OnOpenInFolderRequested;
        _view.OpenInTerminalRequested += OnOpenInTerminalRequested;

        UpdateSyncButtons();
        UpdateRepoActionButtons();

        _subscriptions.Add(_registry.Active.Subscribe(_ => OnRepoOrRefsChanged()));
        _subscriptions.Add(_bus.SubscribeScoped<CommitCreatedMessage>(_ => OnRepoOrRefsChanged()));
        _subscriptions.Add(_bus.SubscribeScoped<RefsChangedMessage>(_ => OnRepoOrRefsChanged()));
    }

    public void Dispose()
    {
        _statusGen.Bump();
        _pushSpinner.Dispose();
        _pullSpinner.Dispose();
        _fetchSpinner.Dispose();
        _subscriptions.Dispose();
        _view.PushRequested -= OnPushRequested;
        _view.PullRequested -= OnPullRequested;
        _view.FetchRequested -= OnFetchRequested;
        _view.OpenInFolderRequested -= OnOpenInFolderRequested;
        _view.OpenInTerminalRequested -= OnOpenInTerminalRequested;
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
        _pushSpinner.Start();

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
                _pushSpinner.Stop();
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
        _pullSpinner.Start();

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
                _pullSpinner.Stop();
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
        _fetchSpinner.Start();

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
                _fetchSpinner.Stop();
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
}
