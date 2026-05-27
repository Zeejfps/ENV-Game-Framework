using ZGF.Observable;

namespace GitGui;

internal sealed class ActionsToolbarViewModel : ViewModelBase<ActionsToolbarState>
{
    private readonly IRepoRegistry _registry;
    private readonly IGitService _gitService;
    private readonly IPlatformShell _shell;
    private readonly IMessageBus _bus;
    private readonly State<ThemeMode> _themeMode;

    private readonly GenerationGuard _statusGen = new();
    private readonly GenerationGuard _localChangesGen = new();

    private readonly SpinnerAnimation _pushSpinner;
    private readonly SpinnerAnimation _pullSpinner;
    private readonly SpinnerAnimation _fetchSpinner;

    public Command Push { get; }
    public Command Pull { get; }
    public Command Fetch { get; }
    public Command Branch { get; }
    public Command Stash { get; }
    public Command OpenFolder { get; }
    public Command OpenTerminal { get; }
    public Command ToggleTheme { get; }

    public IReadable<int?> PushBadge { get; }
    public IReadable<int?> PullBadge { get; }
    public IReadable<bool> IsPushing { get; }
    public IReadable<bool> IsPulling { get; }
    public IReadable<bool> IsFetching { get; }
    public IReadable<string?> Error { get; }
    public IReadable<ThemeMode> Theme => _themeMode;

    public IReadable<float> PushRotation => _pushSpinner.Rotation;
    public IReadable<float> PullRotation => _pullSpinner.Rotation;
    public IReadable<float> FetchRotation => _fetchSpinner.Rotation;

    public ActionsToolbarViewModel(
        IRepoRegistry registry,
        IGitService gitService,
        IPlatformShell shell,
        IUiDispatcher dispatcher,
        IMessageBus bus,
        State<ThemeMode> themeMode)
        : base(dispatcher, ActionsToolbarState.Initial)
    {
        _registry = registry;
        _gitService = gitService;
        _shell = shell;
        _bus = bus;
        _themeMode = themeMode;

        _pushSpinner = new SpinnerAnimation(dispatcher);
        _pullSpinner = new SpinnerAnimation(dispatcher);
        _fetchSpinner = new SpinnerAnimation(dispatcher);

        var repoActionsEnabled = Slice(s => s.HasActiveRepo);
        Push = new Command(DoPush, Slice(ComputePushEnabled));
        Pull = new Command(DoPull, Slice(ComputePullEnabled));
        Fetch = new Command(DoFetch, Slice(s => !s.IsFetching && s.HasActiveRepo));
        Branch = new Command(DoBranch, repoActionsEnabled);
        Stash = new Command(DoStash, Slice(s => s.HasActiveRepo && s.HasLocalChanges));
        OpenFolder = new Command(DoOpenFolder, repoActionsEnabled);
        OpenTerminal = new Command(DoOpenTerminal, repoActionsEnabled);
        ToggleTheme = new Command(DoToggleTheme);

        PushBadge = Slice(ComputePushBadge);
        PullBadge = Slice(ComputePullBadge);
        IsPushing = Slice(s => s.IsPushing);
        IsPulling = Slice(s => s.IsPulling);
        IsFetching = Slice(s => s.IsFetching);
        Error = Slice(s => s.Error);

        Subscriptions.Add(_registry.Active.Subscribe(_ => OnRepoOrRefsChanged()));
        Subscriptions.Add(_bus.SubscribeScoped<CommitCreatedMessage>(_ => OnRepoOrRefsChanged()));
        Subscriptions.Add(_bus.SubscribeScoped<RefsChangedMessage>(_ => OnRepoOrRefsChanged()));
        Subscriptions.Add(_bus.SubscribeScoped<WorkingTreeChangedMessage>(_ => ReloadLocalChanges()));
    }

    private static bool ComputePushEnabled(ActionsToolbarState s)
    {
        var hasBranchUpstream = !s.PushStatus.IsDetached && s.PushStatus.HasUpstream;
        var canPublish = !s.PushStatus.IsDetached && !s.PushStatus.HasUpstream
            && !string.IsNullOrEmpty(s.PushStatus.CurrentBranchName);
        return !s.IsPushing && ((hasBranchUpstream && s.PushStatus.Ahead > 0) || canPublish);
    }

    private static bool ComputePullEnabled(ActionsToolbarState s)
    {
        var hasBranchUpstream = !s.PushStatus.IsDetached && s.PushStatus.HasUpstream;
        return !s.IsPulling && hasBranchUpstream && s.PushStatus.Behind > 0;
    }

    private static int? ComputePushBadge(ActionsToolbarState s)
    {
        if (s.IsPushing) return null;
        var hasBranchUpstream = !s.PushStatus.IsDetached && s.PushStatus.HasUpstream;
        return hasBranchUpstream ? s.PushStatus.Ahead : 0;
    }

    private static int? ComputePullBadge(ActionsToolbarState s)
    {
        if (s.IsPulling) return null;
        var hasBranchUpstream = !s.PushStatus.IsDetached && s.PushStatus.HasUpstream;
        return hasBranchUpstream ? s.PushStatus.Behind : 0;
    }

    private void OnRepoOrRefsChanged()
    {
        var repo = _registry.Active.Value;
        if (repo == null)
        {
            _statusGen.Bump();
            _localChangesGen.Bump();
            Update(_ => ActionsToolbarState.Initial);
            return;
        }
        Update(s => s with { HasActiveRepo = true, Error = null });
        ReloadPushStatus(repo);
        ReloadLocalChanges();
    }

    private void ReloadPushStatus(Repo repo)
    {
        var gen = _statusGen.Bump();
        var service = _gitService;
        var dispatcher = Dispatcher;

        Task.Run(() =>
        {
            var status = service.GetPushStatus(repo);
            dispatcher.Post(() =>
            {
                if (_statusGen.IsStale(gen)) return;
                if (_registry.Active.Value?.Id != repo.Id) return;
                Update(s => s with { PushStatus = status });
            });
        });
    }

    private void ReloadLocalChanges()
    {
        var repo = _registry.Active.Value;
        if (repo == null)
        {
            _localChangesGen.Bump();
            Update(s => s.HasLocalChanges ? s with { HasLocalChanges = false } : s);
            return;
        }

        var gen = _localChangesGen.Bump();
        var service = _gitService;
        var dispatcher = Dispatcher;

        Task.Run(() =>
        {
            var snap = service.GetLocalChanges(repo);
            dispatcher.Post(() =>
            {
                if (_localChangesGen.IsStale(gen)) return;
                if (_registry.Active.Value?.Id != repo.Id) return;
                var hasChanges = snap.Staged.Count + snap.Unstaged.Count > 0;
                Update(s => s.HasLocalChanges == hasChanges ? s : s with { HasLocalChanges = hasChanges });
            });
        });
    }

    private void DoOpenFolder()
    {
        var repo = _registry.Active.Value;
        if (repo == null) return;
        try { _shell.OpenFolder(repo.Path); }
        catch (Exception ex) { Update(s => s with { Error = $"Open folder failed: {ex.Message}" }); }
    }

    private void DoOpenTerminal()
    {
        var repo = _registry.Active.Value;
        if (repo == null) return;
        try { _shell.OpenTerminal(repo.Path); }
        catch (Exception ex) { Update(s => s with { Error = $"Open terminal failed: {ex.Message}" }); }
    }

    private void DoToggleTheme()
    {
        _themeMode.Value = _themeMode.Value == ThemeMode.Dark ? ThemeMode.Light : ThemeMode.Dark;
    }

    private void DoBranch()
    {
        var repo = _registry.Active.Value;
        if (repo == null) return;
        var pushStatus = State.Value.PushStatus;
        // Detached HEAD has no branch name to seed from; "HEAD" still works as a starting
        // ref for `git branch newname HEAD` and matches Fork's default.
        var suggested = pushStatus.IsDetached || string.IsNullOrEmpty(pushStatus.CurrentBranchName)
            ? "HEAD"
            : pushStatus.CurrentBranchName;
        _bus.Broadcast(new ShowDialogMessage(onClose => new CreateBranchDialog(repo, suggested, onClose)));
    }

    private void DoStash()
    {
        var repo = _registry.Active.Value;
        if (repo == null) return;
        _bus.Broadcast(new ShowDialogMessage(onClose => new StashDialog(repo, onClose)));
    }

    private void DoPush()
    {
        var repo = _registry.Active.Value;
        if (repo == null) return;
        var state = State.Value;
        if (state.IsPushing) return;

        var pushStatus = state.PushStatus;
        if (!pushStatus.IsDetached
            && !pushStatus.HasUpstream
            && !string.IsNullOrEmpty(pushStatus.CurrentBranchName))
        {
            var localBranch = pushStatus.CurrentBranchName!;
            _bus.Broadcast(new ShowDialogMessage(onClose => new PublishBranchDialog(
                new PublishBranchRequest(repo, localBranch), onClose)));
            return;
        }

        if (!pushStatus.IsDetached
            && pushStatus.HasUpstream
            && pushStatus.Ahead > 0
            && pushStatus.Behind > 0)
        {
            var branchName = pushStatus.CurrentBranchName ?? string.Empty;
            var ahead = pushStatus.Ahead;
            var behind = pushStatus.Behind;
            _bus.Broadcast(new ShowDialogMessage(onClose => new ForcePushDialog(
                repo, branchName, ahead, behind, onClose)));
            return;
        }

        Update(s => s with { IsPushing = true, Error = null });
        _pushSpinner.Start();

        var service = _gitService;
        var dispatcher = Dispatcher;
        var bus = _bus;

        Task.Run(() =>
        {
            PushOutcome outcome;
            try { outcome = service.Push(repo); }
            catch (Exception ex) { outcome = new PushOutcome(false, ex.Message); }

            dispatcher.Post(() =>
            {
                _pushSpinner.Stop();
                if (!outcome.Success)
                {
                    Update(s => s with { IsPushing = false, Error = outcome.ErrorMessage ?? "Push failed." });
                    return;
                }
                Update(s => s with { IsPushing = false, PushStatus = s.PushStatus with { Ahead = 0 } });
                bus.Broadcast(new RefsChangedMessage(repo.Id));
            });
        });
    }

    private void DoPull()
    {
        var repo = _registry.Active.Value;
        if (repo == null) return;
        var state = State.Value;
        if (state.IsPulling) return;

        Update(s => s with { IsPulling = true, Error = null });
        _pullSpinner.Start();

        var service = _gitService;
        var dispatcher = Dispatcher;
        var bus = _bus;

        Task.Run(() =>
        {
            PullOutcome outcome;
            try { outcome = service.Pull(repo); }
            catch (Exception ex) { outcome = new PullOutcome(false, ex.Message); }

            dispatcher.Post(() =>
            {
                _pullSpinner.Stop();
                if (!outcome.Success)
                {
                    Update(s => s with { IsPulling = false, Error = outcome.ErrorMessage ?? "Pull failed." });
                    return;
                }
                Update(s => s with { IsPulling = false, PushStatus = s.PushStatus with { Behind = 0 } });
                bus.Broadcast(new RefsChangedMessage(repo.Id));
            });
        });
    }

    private void DoFetch()
    {
        var repo = _registry.Active.Value;
        if (repo == null) return;
        var state = State.Value;
        if (state.IsFetching) return;

        Update(s => s with { IsFetching = true, Error = null });
        _fetchSpinner.Start();

        var service = _gitService;
        var dispatcher = Dispatcher;
        var bus = _bus;

        Task.Run(() =>
        {
            FetchOutcome outcome;
            try { outcome = service.Fetch(repo); }
            catch (Exception ex) { outcome = new FetchOutcome(false, ex.Message); }

            dispatcher.Post(() =>
            {
                _fetchSpinner.Stop();
                if (!outcome.Success)
                {
                    Update(s => s with { IsFetching = false, Error = outcome.ErrorMessage ?? "Fetch failed." });
                    return;
                }
                Update(s => s with { IsFetching = false });
                bus.Broadcast(new RefsChangedMessage(repo.Id));
            });
        });
    }

    public override void Dispose()
    {
        _statusGen.Bump();
        _localChangesGen.Bump();
        _pushSpinner.Dispose();
        _pullSpinner.Dispose();
        _fetchSpinner.Dispose();
        base.Dispose();
    }
}