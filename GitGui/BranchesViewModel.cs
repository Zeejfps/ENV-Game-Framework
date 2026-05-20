using ZGF.Observable;

namespace GitGui;

/// <summary>
/// View model for the Branches sidebar. Mirrors the LocalChangesViewModel pattern:
/// state lives in an immutable <see cref="BranchesState"/> record; views subscribe to
/// per-field slices and call command methods to drive interactions and git ops.
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
    private const float IndentSection = 0f;
    private const float IndentRemoteHeader = 12f;
    private const float IndentLocalTreeBase = 16f;
    private const float IndentRemoteTreeBase = 28f;
    private const float IndentStashBase = 16f;
    private const float IndentLevel = 16f;

    private readonly IRepoRegistry _registry;
    private readonly IGitService _gitService;
    private readonly IMessageBus _bus;
    private readonly State<MainViewMode> _mode;

    public IReadable<IReadOnlyList<BranchRow>> Rows { get; }
    public IReadable<BranchSelection?> Selection { get; }
    public IReadable<string?> BusyBranch { get; }
    public IReadable<string?> LoadError { get; }
    public IReadable<bool> IsLoading { get; }

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

        Rows = Slice(s => BuildRows(s.Listing, s.Ui));
        Selection = Slice(s => s.Selection);
        BusyBranch = Slice(s => s.BusyBranch);
        LoadError = Slice(s => s.LoadError);
        IsLoading = Slice(s => s.IsLoading);

        Subscriptions.Add(_registry.Active.Subscribe(_ => OnActiveRepoChanged()));
        Subscriptions.Add(_bus.SubscribeScoped<CommitCreatedMessage>(OnCommitCreated));
        Subscriptions.Add(_bus.SubscribeScoped<CommitSelectedMessage>(OnCommitSelected));
        Subscriptions.Add(_bus.SubscribeScoped<RefsChangedMessage>(OnRefsChanged));
    }

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

        var ui = _registry.GetBranchesUi(active.Id) ?? new BranchesUiState();
        Update(s => s with
        {
            Listing = null,
            Ui = ui,
            Selection = null,
            BusyBranch = null,
            IsBranchOpInFlight = false,
            IsLoading = true,
            LoadError = null,
        });
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
        if (current.HasValue && msg.Sha == current.Value.TipSha) return;
        if (current == null) return;
        Update(s => s with { Selection = null });
    }

    private void StartLoad(Repo repo)
    {
        var gitService = _gitService;
        var gen = Gen.Bump();

        Update(s => s.LoadError != null ? s with { LoadError = null } : s);
        // Same-repo reloads keep IsLoading false so the list doesn't flash a placeholder —
        // only cross-repo switches set IsLoading via OnActiveRepoChanged.

        Task.Run(() =>
        {
            BranchListing listing;
            try
            {
                listing = gitService.GetBranches(repo);
            }
            catch (Exception ex)
            {
                listing = new BranchListing(repo.Id, Array.Empty<BranchEntry>(), Array.Empty<RemoteGroup>(), Array.Empty<StashEntry>(), ex.Message);
            }

            Dispatcher.Post(() =>
            {
                if (Gen.IsStale(gen)) return;
                if (repo.Id != _activeRepoId) return;
                ApplyListing(listing);
            });
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

        // If we cleared selection above, also tell the history view to forget it.
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

    // ---- row interactions ----

    public void OnRowClicked(BranchRow? row)
    {
        if (row == null)
        {
            ClearSelectionAndBroadcast();
            return;
        }

        switch (row.Kind)
        {
            case BranchRowKind.LocalHeader:
                ToggleLocalSection();
                return;
            case BranchRowKind.RemotesHeader:
                ToggleRemotesSection();
                return;
            case BranchRowKind.StashesHeader:
                ToggleStashesSection();
                return;
            case BranchRowKind.RemoteHeader:
                if (row.RemoteName != null) ToggleRemote(row.RemoteName);
                return;
            case BranchRowKind.Folder:
                if (row.FolderKey != null) ToggleFolder(row.FolderKey);
                return;
            case BranchRowKind.LocalBranch:
                if (row.TipSha != null && row.FullPath != null)
                    SelectAndBroadcast(new BranchSelection(IsRemote: false, IsStash: false, RemoteName: null, FullPath: row.FullPath, TipSha: row.TipSha));
                return;
            case BranchRowKind.RemoteBranch:
                if (row.TipSha != null && row.RemoteName != null && row.FullPath != null)
                    SelectAndBroadcast(new BranchSelection(IsRemote: true, IsStash: false, RemoteName: row.RemoteName, FullPath: row.FullPath, TipSha: row.TipSha));
                return;
            case BranchRowKind.Stash:
                if (row.TipSha != null && row.FullPath != null)
                    SelectAndBroadcast(new BranchSelection(IsRemote: false, IsStash: true, RemoteName: null, FullPath: row.FullPath, TipSha: row.TipSha));
                return;
        }
    }

    public void OnRowActivated(BranchRow row)
    {
        if (State.Value.IsBranchOpInFlight) return;
        switch (row.Kind)
        {
            case BranchRowKind.LocalBranch:
                if (row.IsHead) return;
                if (row.FullPath == null) return;
                StartCheckoutLocal(row.FullPath);
                return;
            case BranchRowKind.RemoteBranch:
                if (row.RemoteName == null || row.FullPath == null) return;
                if (LocalBranchExists(row.FullPath))
                {
                    StartCheckoutLocal(row.FullPath);
                }
                else
                {
                    var repo = _registry.Active.Value;
                    if (repo == null) return;
                    _bus.Broadcast(new ShowCheckoutDialogMessage(
                        repo, row.RemoteName, row.FullPath, row.FullPath));
                }
                return;
            case BranchRowKind.Stash:
                if (row.StashIndex is int idx)
                    StartStashApply(idx, row.FullPath ?? $"stash@{{{idx}}}", row.DisplayName);
                return;
        }
    }

    // Builds the right-click menu for a row. Only LocalBranch and RemoteBranch rows get a
    // menu — other rows return an empty list (the controller will not open a menu).
    public IReadOnlyList<RepoBarContextMenu.Item> BuildContextMenuItems(BranchRow row)
    {
        var state = State.Value;
        var repo = _registry.Active.Value;
        if (repo == null) return Array.Empty<RepoBarContextMenu.Item>();

        switch (row.Kind)
        {
            case BranchRowKind.LocalBranch:
            {
                if (row.FullPath == null) return Array.Empty<RepoBarContextMenu.Item>();
                var thisRowBusy = state.BusyBranch == row.FullPath;
                var checkoutDisabled = row.IsHead || state.IsBranchOpInFlight;
                var renameDisabled = thisRowBusy;
                var deleteDisabled = row.IsHead || thisRowBusy;
                var capturedRepo = repo;
                var capturedName = row.FullPath;
                return new[]
                {
                    new RepoBarContextMenu.Item(
                        "Checkout",
                        () => StartCheckoutLocal(capturedName),
                        LucideIcons.Branch,
                        Enabled: !checkoutDisabled),
                    new RepoBarContextMenu.Item(
                        "Rename…",
                        () => _bus.Broadcast(new ShowRenameBranchDialogMessage(capturedRepo, capturedName)),
                        LucideIcons.PencilLine,
                        Enabled: !renameDisabled),
                    new RepoBarContextMenu.Item(
                        "Delete…",
                        () => _bus.Broadcast(new ShowDeleteLocalBranchDialogMessage(capturedRepo, capturedName)),
                        LucideIcons.Trash,
                        Enabled: !deleteDisabled),
                };
            }
            case BranchRowKind.RemoteBranch:
            {
                if (row.RemoteName == null || row.FullPath == null) return Array.Empty<RepoBarContextMenu.Item>();
                var checkoutDisabled = state.IsBranchOpInFlight;
                var capturedRepo = repo;
                var capturedRemote = row.RemoteName;
                var capturedName = row.FullPath;
                return new[]
                {
                    new RepoBarContextMenu.Item(
                        "Checkout",
                        () => ActivateRemoteForCheckout(capturedRemote, capturedName),
                        LucideIcons.Branch,
                        Enabled: !checkoutDisabled),
                    new RepoBarContextMenu.Item(
                        "Delete remote branch…",
                        () => _bus.Broadcast(new ShowDeleteRemoteBranchDialogMessage(capturedRepo, capturedRemote, capturedName)),
                        LucideIcons.Trash),
                };
            }
            default:
                return Array.Empty<RepoBarContextMenu.Item>();
        }
    }

    // ---- section/folder toggles ----

    private void ToggleLocalSection()
    {
        Update(s =>
        {
            var ui = s.Ui.Clone();
            ui.LocalOpen = !ui.LocalOpen;
            return s with { Ui = ui };
        });
        PersistUi();
    }

    private void ToggleRemotesSection()
    {
        Update(s =>
        {
            var ui = s.Ui.Clone();
            ui.RemotesOpen = !ui.RemotesOpen;
            return s with { Ui = ui };
        });
        PersistUi();
    }

    private void ToggleStashesSection()
    {
        Update(s =>
        {
            var ui = s.Ui.Clone();
            ui.StashesOpen = !ui.StashesOpen;
            return s with { Ui = ui };
        });
        PersistUi();
    }

    private void ToggleRemote(string remoteName)
    {
        Update(s =>
        {
            var ui = s.Ui.Clone();
            var current = ui.RemoteOpen.TryGetValue(remoteName, out var v) ? v : true;
            ui.RemoteOpen[remoteName] = !current;
            return s with { Ui = ui };
        });
        PersistUi();
    }

    private void ToggleFolder(string key)
    {
        Update(s =>
        {
            var ui = s.Ui.Clone();
            var current = ui.FolderOpen.TryGetValue(key, out var v) ? v : true;
            ui.FolderOpen[key] = !current;
            return s with { Ui = ui };
        });
        PersistUi();
    }

    private void PersistUi()
    {
        if (_activeRepoId == Guid.Empty) return;
        _registry.SetBranchesUi(_activeRepoId, State.Value.Ui);
    }

    // ---- selection + activation ----

    private void SelectAndBroadcast(BranchSelection selection)
    {
        Update(s => s with { Selection = selection });
        SwitchToHistory();
        _bus.Broadcast(new CommitSelectedMessage(_activeRepoId, selection.TipSha));
    }

    private void ClearSelectionAndBroadcast()
    {
        if (State.Value.Selection == null) return;
        Update(s => s with { Selection = null });
        _bus.Broadcast(new CommitSelectedMessage(_activeRepoId, null));
    }

    private void SwitchToHistory()
    {
        if (_mode.Value == MainViewMode.History) return;
        _mode.Value = MainViewMode.History;
    }

    private bool LocalBranchExists(string name)
    {
        var listing = State.Value.Listing;
        if (listing == null) return false;
        foreach (var b in listing.LocalBranches)
            if (string.Equals(b.Name, name, StringComparison.Ordinal)) return true;
        return false;
    }

    private void ActivateRemoteForCheckout(string remoteName, string branchName)
    {
        if (State.Value.IsBranchOpInFlight) return;
        if (LocalBranchExists(branchName))
        {
            StartCheckoutLocal(branchName);
        }
        else
        {
            var repo = _registry.Active.Value;
            if (repo == null) return;
            _bus.Broadcast(new ShowCheckoutDialogMessage(repo, remoteName, branchName, branchName));
        }
    }

    private void StartCheckoutLocal(string branchName)
    {
        var repo = _registry.Active.Value;
        if (repo == null) return;
        if (State.Value.IsBranchOpInFlight) return;

        Update(s => s with { IsBranchOpInFlight = true, BusyBranch = branchName });

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
                Update(s => s with { IsBranchOpInFlight = false, BusyBranch = null });
                if (outcome.Success)
                    bus.Broadcast(new RefsChangedMessage(repo.Id));
                else
                    bus.Broadcast(new ShowOperationErrorMessage(
                        "Checkout failed",
                        outcome.ErrorMessage ?? "Checkout failed."));
            });
        });
    }

    private void StartStashApply(int index, string label, string subject)
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

    // ---- row building ----

    private static IReadOnlyList<BranchRow> BuildRows(BranchListing? listing, BranchesUiState ui)
    {
        var rows = new List<BranchRow>();
        if (listing == null) return rows;

        rows.Add(new BranchRow(BranchRowKind.LocalHeader, "Local", IndentSection, ui.LocalOpen));
        if (ui.LocalOpen)
        {
            var localTree = BuildTree(listing.LocalBranches);
            EmitTreeRows(rows, localTree, ui, isRemote: false, remoteName: null, IndentLocalTreeBase, depth: 0);
        }

        rows.Add(new BranchRow(BranchRowKind.RemotesHeader, "Remote", IndentSection, ui.RemotesOpen));
        if (ui.RemotesOpen)
        {
            foreach (var rg in listing.Remotes)
            {
                var isOpen = ui.RemoteOpen.TryGetValue(rg.Name, out var v) ? v : true;
                rows.Add(new BranchRow(BranchRowKind.RemoteHeader, rg.Name, IndentRemoteHeader, isOpen)
                {
                    RemoteName = rg.Name,
                });
                if (!isOpen) continue;
                var remoteTree = BuildTree(rg.Branches);
                EmitTreeRows(rows, remoteTree, ui, isRemote: true, rg.Name, IndentRemoteTreeBase, depth: 0);
            }
        }

        if (listing.Stashes.Count > 0)
        {
            rows.Add(new BranchRow(BranchRowKind.StashesHeader, "Stashes", IndentSection, ui.StashesOpen));
            if (ui.StashesOpen)
            {
                foreach (var s in listing.Stashes)
                {
                    var label = $"stash@{{{s.Index}}}";
                    rows.Add(new BranchRow(BranchRowKind.Stash, s.Subject, IndentStashBase, isOpen: false)
                    {
                        TipSha = s.Sha,
                        FullPath = label,
                        StashIndex = s.Index,
                    });
                }
            }
        }
        return rows;
    }

    private static void EmitTreeRows(List<BranchRow> rows, IReadOnlyList<TreeNode> nodes, BranchesUiState ui, bool isRemote, string? remoteName, float treeBase, int depth)
    {
        var indent = treeBase + depth * IndentLevel;
        foreach (var node in nodes)
        {
            if (node.Entry is { } entry)
            {
                rows.Add(new BranchRow(isRemote ? BranchRowKind.RemoteBranch : BranchRowKind.LocalBranch, node.Segment, indent, isOpen: false)
                {
                    TipSha = entry.TipSha,
                    IsHead = entry.IsHead,
                    RemoteName = remoteName,
                    FullPath = entry.Name,
                    AheadBy = entry.AheadBy,
                    BehindBy = entry.BehindBy,
                });
            }
            else
            {
                var key = MakeFolderKey(isRemote, remoteName, node.FullPath);
                var open = ui.FolderOpen.TryGetValue(key, out var v) ? v : true;
                rows.Add(new BranchRow(BranchRowKind.Folder, node.Segment, indent, open)
                {
                    RemoteName = remoteName,
                    FullPath = node.FullPath,
                    FolderKey = key,
                });
                if (open) EmitTreeRows(rows, node.Children, ui, isRemote, remoteName, treeBase, depth + 1);
            }
        }
    }

    private static string MakeFolderKey(bool isRemote, string? remoteName, string path) =>
        isRemote ? $"remote:{remoteName}:{path}" : $"local:{path}";

    private static IReadOnlyList<TreeNode> BuildTree(IReadOnlyList<BranchEntry> branches)
    {
        var root = new TreeNode("", "");
        foreach (var b in branches)
        {
            var segments = b.Name.Split('/');
            var current = root;
            for (var i = 0; i < segments.Length; i++)
            {
                var seg = segments[i];
                var isLeaf = i == segments.Length - 1;
                if (!current.ChildIndex.TryGetValue(seg, out var child))
                {
                    var path = i == 0 ? seg : current.FullPath + "/" + seg;
                    child = new TreeNode(seg, path);
                    current.ChildIndex[seg] = child;
                    current.Children.Add(child);
                }
                if (isLeaf) child.Entry = b;
                current = child;
            }
        }
        SortNode(root);
        return root.Children;
    }

    private static void SortNode(TreeNode node)
    {
        node.Children.Sort((a, b) =>
        {
            var aFolder = a.Entry == null;
            var bFolder = b.Entry == null;
            if (aFolder != bFolder) return aFolder ? -1 : 1;
            return string.Compare(a.Segment, b.Segment, StringComparison.OrdinalIgnoreCase);
        });
        foreach (var c in node.Children) SortNode(c);
    }

    private sealed class TreeNode
    {
        public TreeNode(string segment, string fullPath)
        {
            Segment = segment;
            FullPath = fullPath;
        }
        public string Segment { get; }
        public string FullPath { get; }
        public BranchEntry? Entry { get; set; }
        public Dictionary<string, TreeNode> ChildIndex { get; } = new();
        public List<TreeNode> Children { get; } = new();
    }
}

internal sealed record BranchesState(
    BranchListing? Listing,
    BranchesUiState Ui,
    BranchSelection? Selection,
    string? BusyBranch,
    bool IsBranchOpInFlight,
    bool IsLoading,
    string? LoadError)
{
    public static BranchesState Initial { get; } = new(
        Listing: null,
        Ui: new BranchesUiState(),
        Selection: null,
        BusyBranch: null,
        IsBranchOpInFlight: false,
        IsLoading: false,
        LoadError: null);
}
