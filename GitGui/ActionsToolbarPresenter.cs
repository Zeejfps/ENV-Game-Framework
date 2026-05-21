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
    private readonly GenerationGuard _localChangesGen = new();

    private readonly SpinnerAnimation _pushSpinner;
    private readonly SpinnerAnimation _pullSpinner;
    private readonly SpinnerAnimation _fetchSpinner;

    private PushStatus _pushStatus = new(null, HasUpstream: false, Ahead: 0, Behind: 0, IsDetached: false);
    private bool _hasLocalChanges;
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
        _view.BranchRequested += OnBranchRequested;
        _view.StashRequested += OnStashRequested;

        UpdateSyncButtons();
        UpdateRepoActionButtons();
        UpdateStashButton();

        _subscriptions.Add(_registry.Active.Subscribe(_ => OnRepoOrRefsChanged()));
        _subscriptions.Add(_bus.SubscribeScoped<CommitCreatedMessage>(_ => OnRepoOrRefsChanged()));
        _subscriptions.Add(_bus.SubscribeScoped<RefsChangedMessage>(_ => OnRepoOrRefsChanged()));
        _subscriptions.Add(_bus.SubscribeScoped<WorkingTreeChangedMessage>(_ => ReloadLocalChanges()));
    }

    public void Dispose()
    {
        _statusGen.Bump();
        _localChangesGen.Bump();
        _pushSpinner.Dispose();
        _pullSpinner.Dispose();
        _fetchSpinner.Dispose();
        _subscriptions.Dispose();
        _view.PushRequested -= OnPushRequested;
        _view.PullRequested -= OnPullRequested;
        _view.FetchRequested -= OnFetchRequested;
        _view.OpenInFolderRequested -= OnOpenInFolderRequested;
        _view.OpenInTerminalRequested -= OnOpenInTerminalRequested;
        _view.BranchRequested -= OnBranchRequested;
        _view.StashRequested -= OnStashRequested;
    }

    private void OnRepoOrRefsChanged()
    {
        _view.Error = null;
        UpdateRepoActionButtons();
        ReloadPushStatus();
        ReloadLocalChanges();
    }

    private void UpdateRepoActionButtons()
    {
        _view.RepoActionsEnabled = _registry.Active.Value != null;
    }

    private void UpdateStashButton()
    {
        _view.StashEnabled = _registry.Active.Value != null && _hasLocalChanges;
    }

    private void ReloadLocalChanges()
    {
        var repo = _registry.Active.Value;
        if (repo == null)
        {
            _localChangesGen.Bump();
            _hasLocalChanges = false;
            UpdateStashButton();
            return;
        }

        var gen = _localChangesGen.Bump();
        var service = _gitService;
        var dispatcher = _dispatcher;

        Task.Run(() =>
        {
            var snap = service.GetLocalChanges(repo);
            dispatcher.Post(() =>
            {
                if (_localChangesGen.IsStale(gen)) return;
                if (_registry.Active.Value?.Id != repo.Id) return;
                _hasLocalChanges = snap.Staged.Count + snap.Unstaged.Count > 0;
                UpdateStashButton();
            });
        });
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

    private void OnStashRequested()
    {
        var repo = _registry.Active.Value;
        if (repo == null) return;
        _bus.Broadcast(new ShowStashDialogMessage(repo));
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
        var canPublish = !_pushStatus.IsDetached && !_pushStatus.HasUpstream
            && !string.IsNullOrEmpty(_pushStatus.CurrentBranchName);
        _view.PushEnabled = !_isPushing && ((hasBranchUpstream && _pushStatus.Ahead > 0) || canPublish);
        _view.PullEnabled = !_isPulling && hasBranchUpstream && _pushStatus.Behind > 0;
        _view.FetchEnabled = !_isFetching && _registry.Active.Value != null;

        _view.PushBadge = _isPushing ? null : (hasBranchUpstream ? _pushStatus.Ahead : 0);
        _view.PullBadge = _isPulling ? null : (hasBranchUpstream ? _pushStatus.Behind : 0);

        UpdateBranchChip();
    }

    private void UpdateBranchChip()
    {
        if (_registry.Active.Value == null)
        {
            _view.CurrentBranch = null;
            _view.CurrentBranchDetached = false;
            return;
        }
        if (_pushStatus.IsDetached)
        {
            _view.CurrentBranch = "(detached HEAD)";
            _view.CurrentBranchDetached = true;
            return;
        }
        _view.CurrentBranch = string.IsNullOrEmpty(_pushStatus.CurrentBranchName)
            ? null
            : _pushStatus.CurrentBranchName;
        _view.CurrentBranchDetached = false;
    }

    private void OnPushRequested()
    {
        var repo = _registry.Active.Value;
        if (repo == null) return;
        if (_isPushing) return;

        if (!_pushStatus.IsDetached
            && !_pushStatus.HasUpstream
            && !string.IsNullOrEmpty(_pushStatus.CurrentBranchName))
        {
            _bus.Broadcast(new ShowPublishBranchDialogMessage(repo, _pushStatus.CurrentBranchName!));
            return;
        }

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
