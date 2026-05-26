using ZGF.Gui;
using ZGF.Observable;

namespace GitGui;

internal sealed class ActionsToolbarViewModel : ViewModelBase<ActionsToolbarState>
{
    private readonly IRepoRegistry _registry;
    private readonly IGitService _gitService;
    private readonly IPlatformShell _shell;
    private readonly IMessageBus _bus;

    // Push-status and local-changes loads run on independent triggers (refs vs working
    // tree) and can interleave, so each gets its own generation guard rather than
    // sharing the base class's single Gen — otherwise a later call would invalidate
    // the earlier one's pending continuation.
    private readonly GenerationGuard _statusGen = new();
    private readonly GenerationGuard _localChangesGen = new();

    private readonly SpinnerAnimation _pushSpinner;
    private readonly SpinnerAnimation _pullSpinner;
    private readonly SpinnerAnimation _fetchSpinner;

    public IReadable<bool> PushEnabled { get; }
    public IReadable<bool> PullEnabled { get; }
    public IReadable<bool> FetchEnabled { get; }
    public IReadable<bool> RepoActionsEnabled { get; }
    public IReadable<bool> StashEnabled { get; }
    public IReadable<int?> PushBadge { get; }
    public IReadable<int?> PullBadge { get; }
    public IReadable<bool> IsPushing { get; }
    public IReadable<bool> IsPulling { get; }
    public IReadable<bool> IsFetching { get; }
    public IReadable<string?> Error { get; }

    public IReadable<float> PushRotation => _pushSpinner.Rotation;
    public IReadable<float> PullRotation => _pullSpinner.Rotation;
    public IReadable<float> FetchRotation => _fetchSpinner.Rotation;

    public ActionsToolbarViewModel(
        IRepoRegistry registry,
        IGitService gitService,
        IPlatformShell shell,
        IUiDispatcher dispatcher,
        IMessageBus bus)
        : base(dispatcher, ActionsToolbarState.Initial)
    {
        _registry = registry;
        _gitService = gitService;
        _shell = shell;
        _bus = bus;

        _pushSpinner = new SpinnerAnimation(dispatcher);
        _pullSpinner = new SpinnerAnimation(dispatcher);
        _fetchSpinner = new SpinnerAnimation(dispatcher);

        PushEnabled = Slice(ComputePushEnabled);
        PullEnabled = Slice(ComputePullEnabled);
        FetchEnabled = Slice(s => !s.IsFetching && s.HasActiveRepo);
        RepoActionsEnabled = Slice(s => s.HasActiveRepo);
        StashEnabled = Slice(s => s.HasActiveRepo && s.HasLocalChanges);
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

    public void OpenFolder()
    {
        var repo = _registry.Active.Value;
        if (repo == null) return;
        try { _shell.OpenFolder(repo.Path); }
        catch (Exception ex) { Update(s => s with { Error = $"Open folder failed: {ex.Message}" }); }
    }

    public void OpenTerminal()
    {
        var repo = _registry.Active.Value;
        if (repo == null) return;
        try { _shell.OpenTerminal(repo.Path); }
        catch (Exception ex) { Update(s => s with { Error = $"Open terminal failed: {ex.Message}" }); }
    }

    public void Branch()
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

    public void Stash()
    {
        var repo = _registry.Active.Value;
        if (repo == null) return;
        _bus.Broadcast(new ShowDialogMessage(onClose => new StashDialog(repo, onClose)));
    }

    public void Push()
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

        // Diverged from upstream — a plain push would be rejected as non-fast-forward.
        // Pop a confirmation dialog so the user can opt into a force-push (with lease)
        // instead of seeing a cryptic error. Common after rebasing already-pushed history.
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
                Update(s => s with { IsPushing = false });
                bus.Broadcast(new RefsChangedMessage(repo.Id));
            });
        });
    }

    public void Pull()
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
                Update(s => s with { IsPulling = false });
                bus.Broadcast(new RefsChangedMessage(repo.Id));
            });
        });
    }

    public void Fetch()
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

internal sealed record ActionsToolbarState(
    bool HasActiveRepo,
    PushStatus PushStatus,
    bool HasLocalChanges,
    bool IsPushing,
    bool IsPulling,
    bool IsFetching,
    string? Error)
{
    public static ActionsToolbarState Initial { get; } = new(
        HasActiveRepo: false,
        PushStatus: new PushStatus(null, HasUpstream: false, Ahead: 0, Behind: 0, IsDetached: false),
        HasLocalChanges: false,
        IsPushing: false,
        IsPulling: false,
        IsFetching: false,
        Error: null);
}
