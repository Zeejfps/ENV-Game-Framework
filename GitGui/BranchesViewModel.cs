using ZGF.Observable;

namespace GitGui;

/// <summary>
/// View model for the Branches sidebar. Mirrors the LocalChangesViewModel pattern:
/// state lives in an immutable <see cref="BranchesState"/> record; views subscribe to
/// per-field slices and call command methods to drive interactions and git ops.
///
/// The VM exposes the raw model — listing, UI open-state, selection, busy-branch — and
/// command methods keyed on semantic identifiers (branch full paths, remote names,
/// folder keys). It does not produce render rows: the view derives rows from
/// <see cref="Listing"/> and <see cref="Ui"/> and owns its own layout/copy choices.
///
/// Section/folder open-state is mirrored into <see cref="IRepoRegistry.SetBranchesUi"/>
/// after every toggle so it persists across repo switches.
///
/// A single <see cref="BranchesState.IsBranchOpInFlight"/> flag serializes checkout /
/// rename / delete from the UI's perspective; the per-repo GitService lock serializes at
/// the actual command level. Stash apply has its own flag because it's a different
/// concept the user wouldn't expect to be blocked by a branch op.
/// </summary>
internal sealed class BranchesViewModel : ViewModelBase<BranchesState>
{
    private readonly IRepoRegistry _registry;
    private readonly IGitService _gitService;
    private readonly IMessageBus _bus;
    private readonly State<MainViewMode> _mode;

    public IReadable<BranchListing?> Listing { get; }
    public IReadable<BranchesUiState> Ui { get; }
    public IReadable<BranchSelection?> Selection { get; }
    public IReadable<string?> BusyBranch { get; }
    public IReadable<string?> LoadError { get; }
    public IReadable<bool> IsLoading { get; }
    public IReadable<IReadOnlySet<string>> WorktreeBranches { get; }

    private Guid _activeRepoId;

    // Stash apply uses its own flag — it's a distinct concept from branch ops, and the
    // existing presenter treated it that way. Don't fold it into IsBranchOpInFlight.
    private bool _isStashApplying;

    public BranchesViewModel(
        IRepoRegistry registry,
        IGitService gitService,
        IUiDispatcher dispatcher,
        IMessageBus bus,
        State<MainViewMode> mode)
        : base(dispatcher, BranchesState.Initial)
    {
        _registry = registry;
        _gitService = gitService;
        _bus = bus;
        _mode = mode;

        Listing = Slice(s => s.Listing);
        Ui = Slice(s => s.Ui);
        Selection = Slice(s => s.Selection);
        BusyBranch = Slice(s => s.BusyBranch);
        LoadError = Slice(s => s.LoadError);
        IsLoading = Slice(s => s.IsLoading);
        WorktreeBranches = Slice(s => s.WorktreeBranches);

        Subscriptions.Add(_registry.Active.Subscribe(_ => OnActiveRepoChanged()));
        Subscriptions.Add(_bus.SubscribeScoped<CommitCreatedMessage>(OnCommitCreated));
        Subscriptions.Add(_bus.SubscribeScoped<CommitSelectedMessage>(OnCommitSelected));
        Subscriptions.Add(_bus.SubscribeScoped<RefsChangedMessage>(OnRefsChanged));
        Subscriptions.Add(_bus.SubscribeScoped<WorktreesChangedMessage>(OnWorktreesChanged));
        Subscriptions.Add(_registry.WorktreesChanged.Subscribe(_ => RefreshWorktreeBranches()));
    }

    private void OnWorktreesChanged(WorktreesChangedMessage _) => RefreshWorktreeBranches();

    // Set of local-branch names that are checked out somewhere other than the active row.
    // Used by BranchesView to annotate those branches so the user knows trying to check
    // them out here would conflict. Built from sibling worktrees of the active primary
    // (or, when a worktree is active, from the primary and all other siblings).
    private void RefreshWorktreeBranches()
    {
        var active = _registry.Active.Value;
        if (active is null)
        {
            Update(s => s.WorktreeBranches.Count == 0 ? s : s with { WorktreeBranches = EmptyStringSet });
            return;
        }
        var primaryId = active.ParentRepoId ?? active.Id;

        var set = new HashSet<string>(StringComparer.Ordinal);
        foreach (var r in _registry.Repos)
        {
            if (r.Id == active.Id) continue;
            var rootId = r.ParentRepoId ?? r.Id;
            if (rootId != primaryId) continue;
            // Repo.Branch is populated from `git worktree list` by WorktreeSyncService for
            // both the primary and its worktrees. Detached HEADs leave it null and produce
            // no marker (correct: there's no branch name to take).
            if (!string.IsNullOrEmpty(r.Branch))
                set.Add(r.Branch);
        }
        Update(s => s with { WorktreeBranches = set });
    }

    private static readonly IReadOnlySet<string> EmptyStringSet = new HashSet<string>();


    private void OnActiveRepoChanged()
    {
        var active = _registry.Active.Value;
        _activeRepoId = active?.Id ?? Guid.Empty;

        if (active == null)
        {
            Gen.Bump();
            Update(_ => BranchesState.Initial);
            return;
        }

        var ui = _registry.GetBranchesUi(active.Id);
        Update(_ => new BranchesState(Listing: null, Ui: ui, Selection: null, BusyBranch: null, IsLoading: true, LoadError: null, WorktreeBranches: EmptyStringSet));
        RefreshWorktreeBranches();
        StartLoad(active);
    }

    private void OnCommitCreated(CommitCreatedMessage msg)
    {
        var active = _registry.Active.Value;
        if (active == null || active.Id != msg.RepoId) return;
        StartLoad(active);
    }

    private void OnRefsChanged(RefsChangedMessage msg)
    {
        var active = _registry.Active.Value;
        if (active == null || active.Id != msg.RepoId) return;
        StartLoad(active);
    }

    private void OnCommitSelected(CommitSelectedMessage msg)
    {
        if (msg.RepoId != _activeRepoId) return;
        var current = State.Value.Selection;
        if (current == null) return;
        if (msg.Sha == current.Value.TipSha) return;
        Update(s => s with { Selection = null });
    }

    private void StartLoad(Repo repo)
    {
        Update(s => s.LoadError != null ? s with { LoadError = null } : s);

        RunBackground<BranchListing>(
            work: () => (_gitService.GetBranches(repo), null),
            onResult: (listing, error) =>
            {
                if (repo.Id != _activeRepoId) return;
                // ApplyListing's error path keys off BranchListing.ErrorMessage, so wrap
                // RunBackground's separate `error` channel back into a synthetic listing.
                var applied = error != null
                    ? new BranchListing(repo.Id, Array.Empty<BranchEntry>(), Array.Empty<RemoteGroup>(), Array.Empty<StashEntry>(), error)
                    : listing!;
                ApplyListing(applied);
            });
    }

    private void ApplyListing(BranchListing listing)
    {
        Update(s =>
        {
            // Drop selection if it points at a ref that no longer exists (covers
            // post-delete / post-rename cleanup uniformly — the dialog presenters just
            // broadcast RefsChangedMessage and rely on this check).
            var selection = s.Selection;
            if (selection.HasValue && !RefStillExists(selection.Value, listing))
                selection = null;

            return s with
            {
                Listing = listing.ErrorMessage == null ? listing : null,
                LoadError = listing.ErrorMessage,
                IsLoading = false,
                Selection = selection,
            };
        });

        if (State.Value.Selection == null)
            _bus.Broadcast(new CommitSelectedMessage(_activeRepoId, null));
    }

    private static bool RefStillExists(BranchSelection sel, BranchListing listing)
    {
        if (sel.IsStash)
        {
            foreach (var s in listing.Stashes)
                if ($"stash@{{{s.Index}}}" == sel.FullPath) return true;
            return false;
        }
        if (sel.IsRemote)
        {
            foreach (var rg in listing.Remotes)
            {
                if (rg.Name != sel.RemoteName) continue;
                foreach (var b in rg.Branches)
                    if (b.Name == sel.FullPath) return true;
            }
            return false;
        }
        foreach (var b in listing.LocalBranches)
            if (b.Name == sel.FullPath) return true;
        return false;
    }

    // ---- section/folder toggles ----

    public void ToggleLocalSection() => MutateUi(ui => ui.LocalOpen = !ui.LocalOpen);
    public void ToggleRemotesSection() => MutateUi(ui => ui.RemotesOpen = !ui.RemotesOpen);
    public void ToggleStashesSection() => MutateUi(ui => ui.StashesOpen = !ui.StashesOpen);

    public void ToggleRemote(string remoteName) =>
        MutateUi(ui => ui.RemoteOpen[remoteName] = !ui.RemoteOpen.GetValueOrDefault(remoteName, true));

    public void ToggleFolder(string key) =>
        MutateUi(ui => ui.FolderOpen[key] = !ui.FolderOpen.GetValueOrDefault(key, true));

    private void MutateUi(Action<BranchesUiState> mutate)
    {
        Update(s =>
        {
            var ui = s.Ui.Clone();
            mutate(ui);
            return s with { Ui = ui };
        });
        if (_activeRepoId == Guid.Empty) return;
        _registry.SetBranchesUi(_activeRepoId, State.Value.Ui);
    }

    // ---- selection ----

    public void SelectLocalBranch(string fullPath, string tipSha)
        => SelectAndBroadcast(new BranchSelection(IsRemote: false, IsStash: false, RemoteName: null, FullPath: fullPath, TipSha: tipSha));

    public void SelectRemoteBranch(string remoteName, string fullPath, string tipSha)
        => SelectAndBroadcast(new BranchSelection(IsRemote: true, IsStash: false, RemoteName: remoteName, FullPath: fullPath, TipSha: tipSha));

    public void SelectStash(string stashLabel, string tipSha)
        => SelectAndBroadcast(new BranchSelection(IsRemote: false, IsStash: true, RemoteName: null, FullPath: stashLabel, TipSha: tipSha));

    public void ClearSelection()
    {
        if (State.Value.Selection == null) return;
        Update(s => s with { Selection = null });
        _bus.Broadcast(new CommitSelectedMessage(_activeRepoId, null));
    }

    private void SelectAndBroadcast(BranchSelection selection)
    {
        Update(s => s with { Selection = selection });
        SwitchToHistory();
        _bus.Broadcast(new CommitSelectedMessage(_activeRepoId, selection.TipSha));
    }

    private void SwitchToHistory()
    {
        if (_mode.Value == MainViewMode.History) return;
        _mode.Value = MainViewMode.History;
    }

    // ---- activation (double-click) ----

    public void ActivateLocalBranch(string fullPath, bool isHead)
    {
        if (State.Value.IsBranchOpInFlight) return;
        if (isHead) return;
        // Branch is checked out in a sibling worktree (or in the primary while a worktree
        // is active) — git will refuse the checkout. Surface the sibling instead so the
        // user can switch context with one click rather than reading a fatal: error.
        if (State.Value.WorktreeBranches.Contains(fullPath))
        {
            SwitchToSiblingHoldingBranch(fullPath);
            return;
        }
        StartCheckoutLocal(fullPath);
    }

    private void SwitchToSiblingHoldingBranch(string branchName)
    {
        var active = _registry.Active.Value;
        if (active is null) return;
        var primaryId = active.ParentRepoId ?? active.Id;
        foreach (var r in _registry.Repos)
        {
            if (r.Id == active.Id) continue;
            var rootId = r.ParentRepoId ?? r.Id;
            if (rootId != primaryId) continue;
            if (string.Equals(r.Branch, branchName, StringComparison.Ordinal))
            {
                _registry.SetActive(r.Id);
                return;
            }
        }
    }

    public void ActivateRemoteBranch(string remoteName, string fullPath)
    {
        if (State.Value.IsBranchOpInFlight) return;
        if (LocalBranchExists(fullPath))
        {
            StartCheckoutLocal(fullPath);
            return;
        }
        var repo = _registry.Active.Value;
        if (repo == null) return;
        _bus.Broadcast(new ShowCheckoutDialogMessage(repo, remoteName, fullPath, fullPath));
    }

    public void ActivateStash(int index, string label, string subject)
    {
        var repo = _registry.Active.Value;
        if (repo == null) return;
        if (_isStashApplying) return;

        _isStashApplying = true;

        var service = _gitService;
        var dispatcher = Dispatcher;
        var bus = _bus;

        Task.Run(() =>
        {
            StashOutcome outcome;
            try { outcome = service.ApplyStash(repo, index); }
            catch (Exception ex) { outcome = new StashOutcome(false, ex.Message); }

            dispatcher.Post(() =>
            {
                _isStashApplying = false;
                if (!outcome.Success)
                {
                    bus.Broadcast(new ShowOperationErrorMessage(
                        "Stash apply failed",
                        outcome.ErrorMessage ?? "Stash apply failed."));
                    return;
                }
                bus.Broadcast(new RefsChangedMessage(repo.Id));
                bus.Broadcast(new WorkingTreeChangedMessage(repo.Id));
                if (outcome.HasConflicts) return;
                bus.Broadcast(new ShowDropStashDialogMessage(repo, index, label, subject));
            });
        });
    }

    private bool LocalBranchExists(string name)
    {
        var listing = State.Value.Listing;
        if (listing == null) return false;
        foreach (var b in listing.LocalBranches)
            if (string.Equals(b.Name, name, StringComparison.Ordinal)) return true;
        return false;
    }

    private void StartCheckoutLocal(string branchName)
    {
        var repo = _registry.Active.Value;
        if (repo == null) return;
        if (State.Value.IsBranchOpInFlight) return;

        Update(s => s with { BusyBranch = branchName });

        var service = _gitService;
        var dispatcher = Dispatcher;
        var bus = _bus;

        Task.Run(() =>
        {
            CheckoutOutcome outcome;
            try { outcome = service.CheckoutLocalBranch(repo, branchName); }
            catch (Exception ex) { outcome = new CheckoutOutcome(false, ex.Message); }

            dispatcher.Post(() =>
            {
                Update(s => s with { BusyBranch = null });
                if (outcome.Success)
                    bus.Broadcast(new RefsChangedMessage(repo.Id));
                else
                    bus.Broadcast(new ShowOperationErrorMessage(
                        "Checkout failed",
                        outcome.ErrorMessage ?? "Checkout failed."));
            });
        });
    }

    // ---- context menu items (semantic, keyed on the row's identity) ----

    public IReadOnlyList<RepoBarContextMenu.Item> BuildLocalBranchMenuItems(string fullPath, bool isHead)
    {
        var repo = _registry.Active.Value;
        if (repo == null) return Array.Empty<RepoBarContextMenu.Item>();

        var state = State.Value;
        var thisRowBusy = state.BusyBranch == fullPath;
        var checkedOutElsewhere = state.WorktreeBranches.Contains(fullPath);
        var checkoutDisabled = isHead || state.IsBranchOpInFlight || checkedOutElsewhere;
        var renameDisabled = thisRowBusy;
        var deleteDisabled = isHead || thisRowBusy || checkedOutElsewhere;
        var headBranch = GetHeadBranchName();
        var canMerge = !isHead && headBranch != null && !state.IsBranchOpInFlight;

        var capturedRepo = repo;
        var capturedName = fullPath;
        var items = new List<RepoBarContextMenu.Item>();

        if (checkedOutElsewhere)
        {
            items.Add(new RepoBarContextMenu.Item(
                "Switch to worktree",
                () => SwitchToSiblingHoldingBranch(capturedName),
                LucideIcons.Branch));
        }

        items.Add(new RepoBarContextMenu.Item(
            "Checkout",
            () => StartCheckoutLocal(capturedName),
            LucideIcons.Branch,
            Enabled: !checkoutDisabled));
        if (headBranch != null && !isHead)
        {
            var capturedHead = headBranch;
            items.Add(new RepoBarContextMenu.Item(
                $"Merge {capturedName} into {capturedHead}…",
                () => _bus.Broadcast(new ShowMergeBranchDialogMessage(capturedRepo, capturedName, capturedName, capturedHead)),
                LucideIcons.Branch,
                Enabled: canMerge,
                LabelSegments: BuildMergeSegments(capturedName, capturedHead)));
        }
        items.Add(new RepoBarContextMenu.Item(
            "Rename…",
            () => _bus.Broadcast(new ShowRenameBranchDialogMessage(capturedRepo, capturedName)),
            LucideIcons.PencilLine,
            Enabled: !renameDisabled));
        items.Add(new RepoBarContextMenu.Item(
            "Delete…",
            () => _bus.Broadcast(new ShowDeleteLocalBranchDialogMessage(capturedRepo, capturedName)),
            LucideIcons.Trash,
            Enabled: !deleteDisabled));

        return items;
    }

    public IReadOnlyList<RepoBarContextMenu.Item> BuildRemoteBranchMenuItems(string remoteName, string fullPath)
    {
        var repo = _registry.Active.Value;
        if (repo == null) return Array.Empty<RepoBarContextMenu.Item>();

        var state = State.Value;
        var checkoutDisabled = state.IsBranchOpInFlight;
        var headBranch = GetHeadBranchName();

        var capturedRepo = repo;
        var capturedRemote = remoteName;
        var capturedName = fullPath;
        var items = new List<RepoBarContextMenu.Item>
        {
            new RepoBarContextMenu.Item(
                "Checkout",
                () => ActivateRemoteBranch(capturedRemote, capturedName),
                LucideIcons.Branch,
                Enabled: !checkoutDisabled),
        };

        if (headBranch != null)
        {
            var capturedHead = headBranch;
            var display = $"{capturedRemote}/{capturedName}";
            var sourceRef = display;
            items.Add(new RepoBarContextMenu.Item(
                $"Merge {display} into {capturedHead}…",
                () => _bus.Broadcast(new ShowMergeBranchDialogMessage(capturedRepo, sourceRef, display, capturedHead)),
                LucideIcons.Branch,
                Enabled: !state.IsBranchOpInFlight,
                LabelSegments: BuildMergeSegments(display, capturedHead)));
        }

        items.Add(new RepoBarContextMenu.Item(
            "Delete remote branch…",
            () => _bus.Broadcast(new ShowDeleteRemoteBranchDialogMessage(capturedRepo, capturedRemote, capturedName)),
            LucideIcons.Trash));
        return items;
    }

    private string? GetHeadBranchName()
    {
        var listing = State.Value.Listing;
        if (listing == null) return null;
        foreach (var b in listing.LocalBranches)
            if (b.IsHead) return b.Name;
        return null;
    }

    private const uint MergeMenuBranchAccent = 0xFF7AB7E0;

    private static IReadOnlyList<MenuLabelSegment> BuildMergeSegments(string source, string target) =>
    [
        new MenuLabelSegment("Merge "),
        new MenuLabelSegment(source, MergeMenuBranchAccent, Bold: true),
        new MenuLabelSegment(" into "),
        new MenuLabelSegment(target, MergeMenuBranchAccent, Bold: true),
        new MenuLabelSegment("…"),
    ];
}

internal sealed record BranchesState(
    BranchListing? Listing,
    BranchesUiState Ui,
    BranchSelection? Selection,
    string? BusyBranch,
    bool IsLoading,
    string? LoadError,
    IReadOnlySet<string> WorktreeBranches)
{
    // A branch op is in flight whenever BusyBranch is non-null. Reading state should
    // prefer this property over checking BusyBranch directly for intent clarity.
    public bool IsBranchOpInFlight => BusyBranch != null;

    public static BranchesState Initial { get; } = new(
        Listing: null,
        Ui: new BranchesUiState(),
        Selection: null,
        BusyBranch: null,
        IsLoading: false,
        LoadError: null,
        WorktreeBranches: new HashSet<string>());
}
