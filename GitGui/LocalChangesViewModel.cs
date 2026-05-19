using ZGF.Observable;

namespace GitGui;

/// <summary>
/// View model for the Local Changes feature. State lives in a single immutable
/// <see cref="LocalChangesState"/> record; <see cref="Update"/> is the only mutation
/// primitive. Views subscribe to per-field slices (auto-deduped by equality) and call
/// the command methods to drive git ops. The VM holds no view references.
/// </summary>
internal sealed class LocalChangesViewModel : IDisposable
{
    private const string OpenRepoPlaceholder = "Open a repository to see local changes.";
    private const string LoadingPlaceholder = "Loading…";

    private static readonly IReadOnlyList<FileChange> Empty = Array.Empty<FileChange>();

    private readonly IRepoRegistry _registry;
    private readonly IGitService _gitService;
    private readonly IUiDispatcher _dispatcher;
    private readonly IMessageBus _bus;
    private readonly SubscriptionGroup _subscriptions = new();
    private readonly GenerationGuard _loadGen = new();

    private readonly State<LocalChangesState> _state = new(new LocalChangesState(
        Title: string.Empty,
        Description: string.Empty,
        Amend: false,
        Placeholder: OpenRepoPlaceholder,
        Unstaged: Empty,
        Staged: Empty,
        OpError: null));

    // Slices are constructed in an order that matters for the snapshot-apply transition:
    // _placeholder must fire BEFORE _unstaged/_staged so the snapshot container is
    // re-attached before SelectableFileRowViews are added to the panels (otherwise rows
    // added to a detached parent render blank). State<T>'s Invalidated event fans out to
    // dependents in subscription order, and each Derived registers when it first reads
    // _state.Value during its initial Recompute — so the construction order below is the
    // notification order downstream.
    private readonly Derived<string> _title;
    private readonly Derived<string> _description;
    private readonly Derived<bool> _amend;
    private readonly Derived<string?> _placeholder;
    private readonly Derived<IReadOnlyList<FileChange>> _unstaged;
    private readonly Derived<IReadOnlyList<FileChange>> _staged;
    private readonly Derived<string?> _opError;
    private readonly Derived<bool> _commitEnabled;

    public IReadable<string> Title => _title;
    public IReadable<string> Description => _description;
    public IReadable<bool> Amend => _amend;
    public IReadable<string?> Placeholder => _placeholder;
    public IReadable<IReadOnlyList<FileChange>> Unstaged => _unstaged;
    public IReadable<IReadOnlyList<FileChange>> Staged => _staged;
    public IReadable<string?> OpError => _opError;
    public IReadable<bool> CommitEnabled => _commitEnabled;

    /// <summary>Fired after a successful stage/unstage so the view can re-select the
    /// affected rows on the destination side — the panel-internal selection state is
    /// owned by the view, not the VM.</summary>
    public event Action<DiffSide, IReadOnlyList<string>>? SelectionRequested;

    // Cached so SetAmend(false) can re-derive the displayed staged list without
    // touching git. _stagedFromIndex is whatever GetLocalChanges last returned;
    // _headFiles is the diff of HEAD vs HEAD~1, populated only while amending.
    private IReadOnlyList<FileChange> _stagedFromIndex = Empty;
    private IReadOnlyList<FileChange>? _headFiles;
    private string _preAmendTitle = string.Empty;
    private string _preAmendDescription = string.Empty;

    public LocalChangesViewModel(
        IRepoRegistry registry,
        IGitService gitService,
        IUiDispatcher dispatcher,
        IMessageBus bus)
    {
        _registry = registry;
        _gitService = gitService;
        _dispatcher = dispatcher;
        _bus = bus;

        _title = new Derived<string>(() => _state.Value.Title);
        _description = new Derived<string>(() => _state.Value.Description);
        _amend = new Derived<bool>(() => _state.Value.Amend);
        _placeholder = new Derived<string?>(() => _state.Value.Placeholder);
        _unstaged = new Derived<IReadOnlyList<FileChange>>(() => _state.Value.Unstaged);
        _staged = new Derived<IReadOnlyList<FileChange>>(() => _state.Value.Staged);
        _opError = new Derived<string?>(() => _state.Value.OpError);
        _commitEnabled = new Derived<bool>(() => _state.Value.CommitEnabled);

        _subscriptions.Add(_registry.Active.Subscribe(_ => StartLoadForActiveRepo()));
        _subscriptions.Add(_bus.SubscribeScoped<RefsChangedMessage>(OnRefsChanged));
    }

    public void Dispose()
    {
        // Bump first so any in-flight worker that resolves after Dispose sees a stale gen
        // and exits without touching state or firing notifications.
        _loadGen.Bump();
        _subscriptions.Dispose();
        _commitEnabled.Dispose();
        _opError.Dispose();
        _staged.Dispose();
        _unstaged.Dispose();
        _placeholder.Dispose();
        _amend.Dispose();
        _description.Dispose();
        _title.Dispose();
    }

    public void SetTitle(string value)
        => Update(s => s with { Title = value });

    public void SetDescription(string value)
        => Update(s => s with { Description = value });

    public void SetAmend(bool on)
    {
        if (on)
        {
            _preAmendTitle = _state.Value.Title;
            _preAmendDescription = _state.Value.Description;

            var title = string.Empty;
            var description = string.Empty;
            IReadOnlyList<FileChange> headFiles = Empty;
            var repo = _registry.Active.Value;
            if (repo != null)
            {
                var head = _gitService.GetHeadCommitMessage(repo);
                if (head != null)
                {
                    title = head.Title;
                    description = head.Description;
                }
                headFiles = _gitService.GetHeadCommitFiles(repo);
            }

            _headFiles = headFiles;
            Update(s => s with
            {
                Amend = true,
                Title = title,
                Description = description,
                Staged = ComputeDisplayedStaged(),
            });
        }
        else
        {
            var title = _preAmendTitle;
            var description = _preAmendDescription;
            _preAmendTitle = string.Empty;
            _preAmendDescription = string.Empty;
            _headFiles = null;

            Update(s => s with
            {
                Amend = false,
                Title = title,
                Description = description,
                Staged = ComputeDisplayedStaged(),
            });
        }
    }

    public void Stage(IReadOnlyList<string> paths) => RunIndexOp(paths, isStage: true);

    public void Unstage(IReadOnlyList<string> paths)
    {
        if (paths.Count == 0) return;

        // While amending, the staged panel may include HEAD-only files (not in the
        // index) that the user wants to drop from the amended commit. Those need a
        // reset against HEAD~1; truly-staged files take the normal unstage path.
        if (_headFiles != null && _headFiles.Count > 0)
        {
            var stagedPaths = new HashSet<string>(_stagedFromIndex.Select(f => f.Path));
            List<string>? toUnstage = null;
            List<string>? toResetToParent = null;
            foreach (var p in paths)
            {
                if (stagedPaths.Contains(p))
                    (toUnstage ??= new List<string>()).Add(p);
                else
                    (toResetToParent ??= new List<string>()).Add(p);
            }
            if (toResetToParent != null)
            {
                RunUnstageWithReset(
                    toUnstage ?? (IReadOnlyList<string>)Array.Empty<string>(),
                    toResetToParent);
                return;
            }
        }
        RunIndexOp(paths, isStage: false);
    }

    // Routes through the bus so DialogPresenter owns the modal lifecycle; the dialog's
    // own presenter runs the git op and broadcasts RefsChangedMessage on success, which
    // brings us back through OnRefsChanged to reload the snapshot.
    public void RequestDiscard(IReadOnlyList<string> paths)
    {
        if (paths.Count == 0) return;
        var repo = _registry.Active.Value;
        if (repo == null) return;
        _bus.Broadcast(new ShowDiscardChangesDialogMessage(repo, paths));
    }

    public void Commit()
    {
        var repo = _registry.Active.Value;
        if (repo == null) return;

        // CommitEnabled gates the button on a non-empty title (and, unless amending, at
        // least one staged file), so reaching this point implies the inputs are valid.
        var snapshot = _state.Value;
        var title = snapshot.Title.Trim();
        var description = snapshot.Description.Trim();
        // Standard git format: subject, blank line, body. Skip the blank line when there's
        // no body so the message is just the subject.
        var message = description.Length > 0 ? $"{title}\n\n{description}" : title;
        var amend = snapshot.Amend;

        var gen = _loadGen.Bump();
        var service = _gitService;
        var dispatcher = _dispatcher;
        var bus = _bus;

        Task.Run(() =>
        {
            string? errorMsg = null;
            LocalChangesSnapshot? newSnap = null;
            try
            {
                errorMsg = service.Commit(repo, message, amend);
                if (errorMsg == null)
                {
                    var snap = service.GetLocalChanges(repo);
                    if (snap.ErrorMessage != null) errorMsg = snap.ErrorMessage;
                    else newSnap = snap;
                }
            }
            catch (Exception ex) { errorMsg = ex.Message; }

            dispatcher.Post(() =>
            {
                if (_loadGen.IsStale(gen)) return;
                Update(s => s with { OpError = errorMsg });
                if (errorMsg != null) return;

                // Reset the pre-amend snapshot so toggling amend back off after the commit
                // doesn't restore stale text from a different commit's session.
                _preAmendTitle = string.Empty;
                _preAmendDescription = string.Empty;
                if (_state.Value.Amend)
                {
                    // SetAmend(false) clears the inputs (saved state was just emptied above).
                    SetAmend(false);
                }
                else
                {
                    Update(s => s with { Title = string.Empty, Description = string.Empty });
                }
                if (newSnap != null) ApplySnapshot(newSnap);
                bus.Broadcast(new CommitCreatedMessage(repo.Id));
            });
        });
    }

    private void Update(Func<LocalChangesState, LocalChangesState> reducer)
        => _state.Value = reducer(_state.Value);

    // After checkout, the working tree may differ (index reset, untracked-vs-tracked
    // status flips), so reload from disk for the current repo.
    private void OnRefsChanged(RefsChangedMessage msg)
    {
        var active = _registry.Active.Value;
        if (active == null || active.Id != msg.RepoId) return;
        StartLoadForActiveRepo();
    }

    private void StartLoadForActiveRepo()
    {
        var active = _registry.Active.Value;
        var gen = _loadGen.Bump();

        if (active == null)
        {
            _stagedFromIndex = Empty;
            // Any error from a previous repo's op no longer applies once we switch.
            Update(s => s with
            {
                Placeholder = OpenRepoPlaceholder,
                OpError = null,
                Staged = Empty,
            });
            return;
        }

        Update(s => s with
        {
            Placeholder = LoadingPlaceholder,
            OpError = null,
        });

        var repo = active;
        var service = _gitService;
        var dispatcher = _dispatcher;
        Task.Run(() =>
        {
            LocalChangesSnapshot? snap = null;
            string? errorMsg = null;
            try
            {
                var result = service.GetLocalChanges(repo);
                if (result.ErrorMessage != null) errorMsg = result.ErrorMessage;
                else snap = result;
            }
            catch (Exception ex) { errorMsg = ex.Message; }

            dispatcher.Post(() =>
            {
                if (_loadGen.IsStale(gen)) return;
                if (errorMsg != null)
                {
                    _stagedFromIndex = Empty;
                    Update(s => s with { Placeholder = errorMsg, Staged = Empty });
                    return;
                }
                if (snap != null) ApplySnapshot(snap);
            });
        });
    }

    private void ApplySnapshot(LocalChangesSnapshot snap)
    {
        _stagedFromIndex = snap.Staged;
        // One atomic state transition. Per-field Derived slices fire in construction
        // order — Placeholder before Unstaged/Staged — so the snapshot container is
        // re-attached before the panels receive their files.
        Update(s => s with
        {
            Placeholder = null,
            Unstaged = snap.Unstaged,
            Staged = ComputeDisplayedStaged(),
        });
    }

    private void RunIndexOp(IReadOnlyList<string> paths, bool isStage)
    {
        if (paths.Count == 0) return;
        var repo = _registry.Active.Value;
        if (repo == null) return;

        // Same generation guard as load: bump and capture so any in-flight worker that
        // resolves after a repo switch or another op doesn't clobber a fresher state.
        var gen = _loadGen.Bump();
        var service = _gitService;
        var dispatcher = _dispatcher;

        Task.Run(() =>
        {
            LocalChangesSnapshot? newSnap = null;
            string? errorMsg = null;
            try
            {
                if (isStage) service.Stage(repo, paths);
                else service.Unstage(repo, paths);
                var snap = service.GetLocalChanges(repo);
                if (snap.ErrorMessage != null) errorMsg = snap.ErrorMessage;
                else newSnap = snap;
            }
            catch (Exception ex) { errorMsg = ex.Message; }

            dispatcher.Post(() =>
            {
                if (_loadGen.IsStale(gen)) return;
                Update(s => s with { OpError = errorMsg });
                // Keep the prior snapshot rendered on failure — losing the list on every
                // transient error would erase the user's selection and context.
                if (newSnap != null)
                {
                    ApplySnapshot(newSnap);
                    // The operated-on rows just moved sides; re-select them on the
                    // destination so the user keeps their place across a stage/unstage.
                    SelectionRequested?.Invoke(isStage ? DiffSide.Staged : DiffSide.Unstaged, paths);
                }
            });
        });
    }

    private void RunUnstageWithReset(IReadOnlyList<string> toUnstage, IReadOnlyList<string> toResetToParent)
    {
        var repo = _registry.Active.Value;
        if (repo == null) return;

        var gen = _loadGen.Bump();
        var service = _gitService;
        var dispatcher = _dispatcher;

        Task.Run(() =>
        {
            LocalChangesSnapshot? newSnap = null;
            string? errorMsg = null;
            try
            {
                if (toUnstage.Count > 0) service.Unstage(repo, toUnstage);
                if (toResetToParent.Count > 0) service.ResetToParent(repo, toResetToParent);
                var snap = service.GetLocalChanges(repo);
                if (snap.ErrorMessage != null) errorMsg = snap.ErrorMessage;
                else newSnap = snap;
            }
            catch (Exception ex) { errorMsg = ex.Message; }

            dispatcher.Post(() =>
            {
                if (_loadGen.IsStale(gen)) return;
                Update(s => s with { OpError = errorMsg });
                if (newSnap != null)
                {
                    ApplySnapshot(newSnap);
                    // Both batches land in unstaged after the reset/unstage.
                    var combined = new List<string>(toUnstage.Count + toResetToParent.Count);
                    combined.AddRange(toUnstage);
                    combined.AddRange(toResetToParent);
                    SelectionRequested?.Invoke(DiffSide.Unstaged, combined);
                }
            });
        });
    }

    // Outside amend mode the displayed staged list is just whatever the index says.
    // While amending we also surface HEAD's files (so the user can see — and optionally
    // remove — files that will otherwise carry over into the amended commit). For files
    // that appear in both lists, the index entry wins so the badge reflects the *current*
    // change rather than the previous-commit change.
    private IReadOnlyList<FileChange> ComputeDisplayedStaged()
    {
        if (_headFiles == null || _headFiles.Count == 0)
            return _stagedFromIndex;

        var seen = new HashSet<string>(_stagedFromIndex.Select(f => f.Path));
        var merged = new List<FileChange>(_stagedFromIndex.Count + _headFiles.Count);
        merged.AddRange(_stagedFromIndex);
        foreach (var h in _headFiles)
        {
            if (seen.Add(h.Path))
                merged.Add(h);
        }
        merged.Sort(static (a, b) => string.Compare(a.Path, b.Path, StringComparison.OrdinalIgnoreCase));
        return merged;
    }
}

internal readonly record struct LocalChangesState(
    string Title,
    string Description,
    bool Amend,
    string? Placeholder,
    IReadOnlyList<FileChange> Unstaged,
    IReadOnlyList<FileChange> Staged,
    string? OpError)
{
    // Amend can be a message-only edit of the previous commit, so it doesn't need
    // anything staged; a regular commit does.
    public bool CommitEnabled =>
        HasNonWhitespace(Title) && (Amend || Staged.Count > 0);

    private static bool HasNonWhitespace(string s)
    {
        foreach (var ch in s)
            if (!char.IsWhiteSpace(ch)) return true;
        return false;
    }
}
