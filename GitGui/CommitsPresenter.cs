using ZGF.Observable;

namespace GitGui;

internal sealed class CommitsPresenter : IDisposable
{
    private const int MaxCommits = 3000;

    private readonly ICommitsView _view;
    private readonly IRepoRegistry _registry;
    private readonly IGitService _gitService;
    private readonly IUiDispatcher _dispatcher;
    private readonly IMessageBus _bus;
    private readonly SubscriptionGroup _subscriptions = new();
    private readonly GenerationGuard _loadGen = new();

    // Holds the most recent *successfully* loaded snapshot, or null if we have no good
    // data for the active repo (no repo, in-flight first load, or last load errored).
    // Soft-refresh and SHA-existence checks both rely on this invariant — never assign
    // an error snapshot here.
    private CommitSnapshot? _snapshot;
    private Guid _loadingRepoId;
    private string? _selectedSha;
    private bool _shouldRebroadcastSelection;
    private bool _isCheckingOutCommit;

    public CommitsPresenter(
        ICommitsView view,
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

        _view.CommitClicked += OnCommitClicked;
        _view.CheckoutCommitRequested += OnCheckoutCommitRequested;

        _subscriptions.Add(_registry.Active.Subscribe(_ => StartLoadForActiveRepo()));
        _subscriptions.Add(_bus.SubscribeScoped<CommitCreatedMessage>(m => ReloadIfActiveRepo(m.RepoId)));
        _subscriptions.Add(_bus.SubscribeScoped<CommitSelectedMessage>(OnCommitSelected));
        _subscriptions.Add(_bus.SubscribeScoped<RefsChangedMessage>(m => ReloadIfActiveRepo(m.RepoId)));

        // CommitsView preserves its visual selection across mode-switch remounts; the
        // presenters don't. Adopt the carried-over SHA so the next snapshot load
        // rebroadcasts it, letting the freshly-spawned CommitDetailsPresenter populate.
        _selectedSha = _view.SelectedSha;
        _shouldRebroadcastSelection = _selectedSha != null;
    }

    public void Dispose()
    {
        _loadGen.Bump();
        _subscriptions.Dispose();
        _view.CommitClicked -= OnCommitClicked;
        _view.CheckoutCommitRequested -= OnCheckoutCommitRequested;
    }

    private void ReloadIfActiveRepo(Guid repoId)
    {
        var active = _registry.Active.Value;
        if (active == null || active.Id != repoId) return;
        StartLoadForActiveRepo();
    }

    // External selection requests (e.g. BranchesView tip clicks). Self-broadcasts come
    // back through here too; we dedupe against _selectedSha so the round trip is harmless.
    private void OnCommitSelected(CommitSelectedMessage msg)
    {
        if (_snapshot == null || _snapshot.RepoId != msg.RepoId) return;
        if (_selectedSha == msg.Sha) return;
        _selectedSha = msg.Sha;
        _view.SetSelectedSha(msg.Sha);
    }

    private void OnCommitClicked(string sha)
    {
        if (_snapshot == null) return;
        if (_selectedSha == sha) return;
        _selectedSha = sha;
        _view.SetSelectedSha(sha);
        _bus.Broadcast(new CommitSelectedMessage(_snapshot.RepoId, sha));
    }

    // Smart flow for "Reset <branch> to here":
    //   - Probe the working tree off-thread.
    //   - Clean tree → reset --hard immediately (no prompt; nothing local to lose).
    //   - Any staged/unstaged change → open ResetCommitDialog so the user picks
    //     soft/mixed/hard explicitly (each preserves a different slice of the dirty state).
    private void OnCheckoutCommitRequested(string sha)
    {
        if (_isCheckingOutCommit) return;
        var snap = _snapshot;
        if (snap == null) return;
        var repo = _registry.Active.Value;
        if (repo == null || repo.Id != snap.RepoId) return;

        _isCheckingOutCommit = true;
        var capturedRepo = repo;
        var capturedSha = sha;
        var service = _gitService;
        var dispatcher = _dispatcher;
        var bus = _bus;

        Task.Run(() =>
        {
            int staged = 0;
            int unstaged = 0;
            string? probeError = null;
            try
            {
                var changes = service.GetLocalChanges(capturedRepo);
                if (changes.ErrorMessage != null)
                {
                    probeError = changes.ErrorMessage;
                }
                else
                {
                    staged = changes.Staged.Count;
                    unstaged = changes.Unstaged.Count;
                }
            }
            catch (Exception ex)
            {
                probeError = ex.Message;
            }

            if (probeError != null)
            {
                dispatcher.Post(() =>
                {
                    _isCheckingOutCommit = false;
                    bus.Broadcast(new ShowOperationErrorMessage("Reset failed", probeError));
                });
                return;
            }

            var clean = staged == 0 && unstaged == 0;
            if (clean)
            {
                ResetOutcome outcome;
                try { outcome = service.ResetCurrent(capturedRepo, capturedSha, ResetMode.Hard); }
                catch (Exception ex) { outcome = new ResetOutcome(false, ex.Message); }

                dispatcher.Post(() =>
                {
                    _isCheckingOutCommit = false;
                    if (outcome.Success)
                    {
                        bus.Broadcast(new RefsChangedMessage(capturedRepo.Id));
                        bus.Broadcast(new WorkingTreeChangedMessage(capturedRepo.Id));
                    }
                    else
                    {
                        bus.Broadcast(new ShowOperationErrorMessage(
                            "Reset failed",
                            outcome.ErrorMessage ?? "Reset failed."));
                    }
                });
                return;
            }

            var shortSha = capturedSha.Length >= 7 ? capturedSha[..7] : capturedSha;
            var capturedStaged = staged;
            var capturedUnstaged = unstaged;
            var capturedSummary = LookupSummary(snap, capturedSha) ?? string.Empty;
            var capturedBranch = snap.HeadBranchName;
            dispatcher.Post(() =>
            {
                _isCheckingOutCommit = false;
                bus.Broadcast(new ShowDialogMessage(onClose => new ResetCommitDialog(
                    capturedRepo, capturedSha, shortSha, capturedSummary, capturedBranch,
                    capturedStaged, capturedUnstaged, onClose)));
            });
        });
    }

    private void StartLoadForActiveRepo()
    {
        var active = _registry.Active.Value;
        var gen = _loadGen.Bump();

        if (active == null)
        {
            _snapshot = null;
            ClearSelectionAndBroadcast();
            _view.SetViewModel(new CommitsViewModel.NoRepo());
            return;
        }

        // Soft refresh: when we already have a snapshot for this repo (e.g. after a tab
        // round-trip, or after a commit/push), keep it visible while a fresh one loads in
        // the background. Avoids a "Loading…" flash and preserves scroll/selection.
        var isSoftRefresh = _snapshot != null && _snapshot.RepoId == active.Id;
        if (!isSoftRefresh)
        {
            _snapshot = null;
            ClearSelectionAndBroadcast();
            _view.SetViewModel(new CommitsViewModel.Loading());
        }
        _loadingRepoId = active.Id;

        var repo = active;
        var service = _gitService;
        var dispatcher = _dispatcher;
        Task.Run(() =>
        {
            CommitSnapshot snap;
            try
            {
                snap = service.Load(repo, MaxCommits);
            }
            catch (Exception ex)
            {
                snap = new CommitSnapshot(repo.Id, repo.Path, Array.Empty<CommitNode>(), 0, false, ex.Message);
            }

            dispatcher.Post(() =>
            {
                if (_loadGen.IsStale(gen)) return;
                ApplyLoadedSnapshot(snap);
            });
        });
    }

    private void ApplyLoadedSnapshot(CommitSnapshot snap)
    {
        if (snap.RepoId != _loadingRepoId) return;

        if (snap.ErrorMessage != null)
        {
            // Drop any prior successful snapshot so the next reload shows "Loading…"
            // rather than silently soft-refreshing on top of an Error placeholder.
            _snapshot = null;
            _view.SetViewModel(new CommitsViewModel.Error(snap.ErrorMessage));
        }
        else
        {
            _snapshot = snap;
            _view.SetViewModel(new CommitsViewModel.Loaded(snap));
            // Selection survives only if the commit still exists in the new snapshot
            // (e.g. it may have been pruned by a rebase or reset).
            if (_selectedSha != null && !SnapshotContainsSha(snap, _selectedSha))
                ClearSelectionAndBroadcast();
            else if (_shouldRebroadcastSelection && _selectedSha != null)
                _bus.Broadcast(new CommitSelectedMessage(snap.RepoId, _selectedSha));
            _shouldRebroadcastSelection = false;
        }

        _bus.Broadcast(new CommitsLoadedMessage(snap.RepoId));
    }

    // Broadcasts against _loadingRepoId — that's the *previous* repo at the moment we
    // clear, which is what subscribers expect ("the prev repo's selection is now gone").
    private void ClearSelectionAndBroadcast()
    {
        if (_selectedSha == null) return;
        _selectedSha = null;
        _view.SetSelectedSha(null);
        _bus.Broadcast(new CommitSelectedMessage(_loadingRepoId, null));
    }

    private static bool SnapshotContainsSha(CommitSnapshot snap, string sha)
    {
        for (var i = 0; i < snap.Commits.Count; i++)
        {
            if (snap.Commits[i].Sha == sha) return true;
        }
        return false;
    }

    private static string? LookupSummary(CommitSnapshot snap, string sha)
    {
        for (var i = 0; i < snap.Commits.Count; i++)
        {
            if (snap.Commits[i].Sha == sha) return snap.Commits[i].Summary;
        }
        return null;
    }
}
