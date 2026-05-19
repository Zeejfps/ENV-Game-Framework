using ZGF.Observable;

namespace GitGui;

internal sealed class BranchesPresenter : IDisposable
{
    private const float IndentSection = 0f;          // LOCAL / REMOTES
    private const float IndentRemoteHeader = 12f;    // origin (under REMOTES)
    private const float IndentLocalTreeBase = 16f;   // depth-0 row under LOCAL
    private const float IndentRemoteTreeBase = 28f;  // depth-0 row under a remote header
    private const float IndentLevel = 16f;           // per-depth step within the tree

    private readonly IBranchesView _view;
    private readonly IRepoRegistry _registry;
    private readonly IGitService _gitService;
    private readonly IUiDispatcher _dispatcher;
    private readonly IMessageBus _bus;
    private readonly State<MainViewMode> _mode;
    private readonly SubscriptionGroup _subscriptions = new();
    private readonly GenerationGuard _loadGen = new();

    private Guid _activeRepoId;
    private BranchListing? _listing;
    private BranchesUiState _ui = new();
    private BranchSelection? _selection;

    // Set while a `git checkout` we triggered is in flight. We ignore further activations
    // (the GitService per-repo lock would queue them anyway, but blocking at the UI is less
    // surprising than silently queueing) and dim the target row so the user has something
    // to look at while the CLI works.
    private bool _isCheckingOut;
    private string? _checkingOutBranchName;

    public BranchesPresenter(
        IBranchesView view,
        IRepoRegistry registry,
        IGitService gitService,
        IUiDispatcher dispatcher,
        IMessageBus bus,
        State<MainViewMode> mode)
    {
        _view = view;
        _registry = registry;
        _gitService = gitService;
        _dispatcher = dispatcher;
        _bus = bus;
        _mode = mode;

        _view.RowClicked += OnRowClicked;
        _view.RowActivated += OnRowActivated;

        _subscriptions.Add(_registry.Active.Subscribe(_ => OnActiveRepoChanged()));
        _subscriptions.Add(_bus.SubscribeScoped<CommitCreatedMessage>(OnCommitCreated));
        _subscriptions.Add(_bus.SubscribeScoped<CommitSelectedMessage>(OnCommitSelected));
        _subscriptions.Add(_bus.SubscribeScoped<RefsChangedMessage>(OnRefsChanged));
    }

    public void Dispose()
    {
        _loadGen.Bump();
        _subscriptions.Dispose();
        _view.RowClicked -= OnRowClicked;
        _view.RowActivated -= OnRowActivated;
    }

    private void OnActiveRepoChanged()
    {
        var active = _registry.Active.Value;
        _activeRepoId = active?.Id ?? Guid.Empty;
        _selection = null;
        _view.SetSelection(null);

        if (active == null)
        {
            _listing = null;
            _ui = new BranchesUiState();
            _view.SetLoadError(null);
            PushRows();
            return;
        }

        _ui = _registry.GetBranchesUi(active.Id) ?? new BranchesUiState();
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

    // Clear the sidebar selection when commit selection moves to anything other than the
    // currently-selected branch's tip. Self-broadcasts (same SHA) short-circuit.
    private void OnCommitSelected(CommitSelectedMessage msg)
    {
        if (msg.RepoId != _activeRepoId) return;
        if (_selection.HasValue && msg.Sha == _selection.Value.TipSha) return;
        _selection = null;
        _view.SetSelection(null);
    }

    private void StartLoad(Repo repo)
    {
        var gitService = _gitService;
        var dispatcher = _dispatcher;
        var gen = _loadGen.Bump();

        Task.Run(() =>
        {
            BranchListing listing;
            try
            {
                listing = gitService.GetBranches(repo);
            }
            catch (Exception ex)
            {
                listing = new BranchListing(repo.Id, Array.Empty<BranchEntry>(), Array.Empty<RemoteGroup>(), ex.Message);
            }

            dispatcher.Post(() =>
            {
                if (_loadGen.IsStale(gen)) return;
                if (repo.Id != _activeRepoId) return;
                _listing = listing.ErrorMessage == null ? listing : null;
                _view.SetLoadError(listing.ErrorMessage);
                PushRows();
            });
        });
    }

    private void PushRows()
    {
        var rows = new List<BranchRow>();
        var listing = _listing;
        if (listing != null)
        {
            rows.Add(new BranchRow(BranchRowKind.LocalHeader, "Local", IndentSection, _ui.LocalOpen));
            if (_ui.LocalOpen)
            {
                var localTree = BuildTree(listing.LocalBranches);
                EmitTreeRows(rows, localTree, isRemote: false, remoteName: null, IndentLocalTreeBase, depth: 0);
            }

            rows.Add(new BranchRow(BranchRowKind.RemotesHeader, "Remote", IndentSection, _ui.RemotesOpen));
            if (_ui.RemotesOpen)
            {
                foreach (var rg in listing.Remotes)
                {
                    var isOpen = _ui.RemoteOpen.TryGetValue(rg.Name, out var v) ? v : true;
                    rows.Add(new BranchRow(BranchRowKind.RemoteHeader, rg.Name, IndentRemoteHeader, isOpen)
                    {
                        RemoteName = rg.Name,
                    });
                    if (!isOpen) continue;
                    var remoteTree = BuildTree(rg.Branches);
                    EmitTreeRows(rows, remoteTree, isRemote: true, rg.Name, IndentRemoteTreeBase, depth: 0);
                }
            }
        }

        _view.SetRows(rows);
    }

    private void EmitTreeRows(List<BranchRow> rows, IReadOnlyList<TreeNode> nodes, bool isRemote, string? remoteName, float treeBase, int depth)
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
                var open = _ui.FolderOpen.TryGetValue(key, out var v) ? v : true;
                rows.Add(new BranchRow(BranchRowKind.Folder, node.Segment, indent, open)
                {
                    RemoteName = remoteName,
                    FullPath = node.FullPath,
                    FolderKey = key,
                });
                if (open) EmitTreeRows(rows, node.Children, isRemote, remoteName, treeBase, depth + 1);
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

    // Folders first, then leaves; alphabetical within each group. The leaf-of-the-same-path
    // case (a branch named "feature" alongside "feature/login") cannot occur in git, so we
    // don't try to handle a node that's simultaneously a folder and a branch.
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

    private void OnRowClicked(BranchRow? row)
    {
        if (row == null)
        {
            ClearSelectionAndBroadcast();
            return;
        }

        switch (row.Kind)
        {
            case BranchRowKind.LocalHeader:
                _ui.LocalOpen = !_ui.LocalOpen;
                PersistUi();
                PushRows();
                return;
            case BranchRowKind.RemotesHeader:
                _ui.RemotesOpen = !_ui.RemotesOpen;
                PersistUi();
                PushRows();
                return;
            case BranchRowKind.RemoteHeader:
                if (row.RemoteName != null)
                {
                    var current = _ui.RemoteOpen.TryGetValue(row.RemoteName, out var v) ? v : true;
                    _ui.RemoteOpen[row.RemoteName] = !current;
                    PersistUi();
                    PushRows();
                }
                return;
            case BranchRowKind.Folder:
                if (row.FolderKey != null)
                {
                    var current = _ui.FolderOpen.TryGetValue(row.FolderKey, out var v) ? v : true;
                    _ui.FolderOpen[row.FolderKey] = !current;
                    PersistUi();
                    PushRows();
                }
                return;
            case BranchRowKind.LocalBranch:
                if (row.TipSha != null && row.FullPath != null)
                {
                    _selection = new BranchSelection(IsRemote: false, RemoteName: null, FullPath: row.FullPath, TipSha: row.TipSha);
                    _view.SetSelection(_selection);
                    SwitchToHistory();
                    _bus.Broadcast(new CommitSelectedMessage(_activeRepoId, row.TipSha));
                }
                return;
            case BranchRowKind.RemoteBranch:
                if (row.TipSha != null && row.RemoteName != null && row.FullPath != null)
                {
                    _selection = new BranchSelection(IsRemote: true, RemoteName: row.RemoteName, FullPath: row.FullPath, TipSha: row.TipSha);
                    _view.SetSelection(_selection);
                    SwitchToHistory();
                    _bus.Broadcast(new CommitSelectedMessage(_activeRepoId, row.TipSha));
                }
                return;
        }
    }

    private void OnRowActivated(BranchRow row)
    {
        // Block double-click activations while a checkout we own is in flight; the GitService
        // lock would queue the next one safely but the user would see no feedback and might
        // queue several more before the first completes.
        if (_isCheckingOut) return;
        switch (row.Kind)
        {
            case BranchRowKind.LocalBranch:
                if (row.IsHead) return; // already checked out
                if (row.FullPath == null) return;
                StartCheckoutLocal(row.FullPath);
                return;
            case BranchRowKind.RemoteBranch:
                if (row.RemoteName == null || row.FullPath == null) return;
                // entry.Name for remote branches is already stripped of the remote prefix
                // (see GitService.GetBranches), so it's the suggested local name as-is.
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
        }
    }

    private bool LocalBranchExists(string name)
    {
        var listing = _listing;
        if (listing == null) return false;
        foreach (var b in listing.LocalBranches)
            if (string.Equals(b.Name, name, StringComparison.Ordinal)) return true;
        return false;
    }

    private void StartCheckoutLocal(string branchName)
    {
        var repo = _registry.Active.Value;
        if (repo == null) return;
        if (_isCheckingOut) return;

        _isCheckingOut = true;
        _checkingOutBranchName = branchName;
        _view.SetBusyBranch(branchName);

        var service = _gitService;
        var dispatcher = _dispatcher;
        var bus = _bus;

        Task.Run(() =>
        {
            CheckoutOutcome outcome;
            try { outcome = service.CheckoutLocalBranch(repo, branchName); }
            catch (Exception ex) { outcome = new CheckoutOutcome(false, ex.Message); }

            dispatcher.Post(() =>
            {
                _isCheckingOut = false;
                _checkingOutBranchName = null;
                _view.SetBusyBranch(null);
                if (outcome.Success)
                    bus.Broadcast(new RefsChangedMessage(repo.Id));
                else
                    bus.Broadcast(new ShowCheckoutErrorMessage(
                        outcome.ErrorMessage ?? "Checkout failed."));
            });
        });
    }

    private void SwitchToHistory()
    {
        if (_mode.Value == MainViewMode.History) return;
        _mode.Value = MainViewMode.History;
    }

    private void ClearSelectionAndBroadcast()
    {
        if (_selection == null) return;
        _selection = null;
        _view.SetSelection(null);
        _bus.Broadcast(new CommitSelectedMessage(_activeRepoId, null));
    }

    private void PersistUi()
    {
        if (_activeRepoId == Guid.Empty) return;
        _registry.SetBranchesUi(_activeRepoId, _ui);
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
