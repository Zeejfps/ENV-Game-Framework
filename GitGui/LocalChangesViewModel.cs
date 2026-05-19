using ZGF.Observable;

namespace GitGui;

/// <summary>
/// View model for the Local Changes feature. Owns observable state for the commit bar
/// (title / description / amend / error / commit-enabled) and for the snapshot
/// (placeholder / unstaged list / staged list), plus the commands that drive git ops.
/// Views bind to the observables and call the commands — there is no view interface and
/// the VM holds no reference to a view.
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

    private readonly State<string?> _placeholder = new(OpenRepoPlaceholder);
    private readonly State<IReadOnlyList<FileChange>> _unstaged = new(Empty);
    private readonly State<IReadOnlyList<FileChange>> _staged = new(Empty);
    private readonly State<string?> _opError = new(null);

    public State<string> Title { get; } = new(string.Empty);
    public State<string> Description { get; } = new(string.Empty);
    public State<bool> Amend { get; } = new(false);

    public IReadable<string?> Placeholder => _placeholder;
    public IReadable<IReadOnlyList<FileChange>> Unstaged => _unstaged;
    public IReadable<IReadOnlyList<FileChange>> Staged => _staged;
    public IReadable<string?> OpError => _opError;
    public IReadable<bool> CommitEnabled { get; }

    /// <summary>Fired after a successful stage/unstage so the view can re-select the
    /// affected rows on the destination side — the panel-internal selection state is
    /// owned by the view, not the VM.</summary>
    public event Action<DiffSide, IReadOnlyList<string>>? SelectionRequested;

    // Cached so OnAmendToggled can re-derive the displayed staged list without
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

        // Amend can be a message-only edit of the previous commit, so it doesn't need
        // anything staged; a regular commit does.
        CommitEnabled = new Derived<bool>(() =>
            HasNonWhitespace(Title.Value) && (Amend.Value || _staged.Value.Count > 0));

        _subscriptions.Add(Amend.Subscribe(_ => OnAmendToggled()));
        _subscriptions.Add(_registry.Active.Subscribe(_ => StartLoadForActiveRepo()));
        _subscriptions.Add(_bus.SubscribeScoped<RefsChangedMessage>(OnRefsChanged));
    }

    public void Dispose()
    {
        _loadGen.Bump();
        _subscriptions.Dispose();
        (CommitEnabled as IDisposable)?.Dispose();
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
                RunUnstageWithReset(toUnstage ?? (IReadOnlyList<string>)Array.Empty<string>(), toResetToParent);
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
        var title = Title.Value.Trim();
        var description = Description.Value.Trim();
        // Standard git format: subject, blank line, body. Skip the blank line when there's
        // no body so the message is just the subject.
        var message = description.Length > 0 ? $"{title}\n\n{description}" : title;
        var amend = Amend.Value;

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
                _opError.Value = errorMsg;
                if (errorMsg != null) return;

                // Reset the pre-amend snapshot so toggling amend back off after the commit
                // doesn't restore stale text from a different commit's session.
                _preAmendTitle = string.Empty;
                _preAmendDescription = string.Empty;
                if (Amend.Value)
                {
                    // Flipping fires the Amend handler which clears the inputs (saved
                    // state was just emptied above).
                    Amend.Value = false;
                }
                else
                {
                    Title.Value = string.Empty;
                    Description.Value = string.Empty;
                }
                if (newSnap != null) ApplySnapshot(newSnap);
                bus.Broadcast(new CommitCreatedMessage(repo.Id));
            });
        });
    }

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
        // Any error from a previous repo's op no longer applies once we switch/reload.
        _opError.Value = null;

        if (active == null)
        {
            _placeholder.Value = OpenRepoPlaceholder;
            _stagedFromIndex = Empty;
            _staged.Value = Empty;
            return;
        }

        _placeholder.Value = LoadingPlaceholder;

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
                    _placeholder.Value = errorMsg;
                    _stagedFromIndex = Empty;
                    _staged.Value = Empty;
                    return;
                }
                if (snap != null) ApplySnapshot(snap);
            });
        });
    }

    private void ApplySnapshot(LocalChangesSnapshot snap)
    {
        _stagedFromIndex = snap.Staged;
        // Placeholder→null first so the view re-attaches the snapshot container before
        // the panels receive their files — otherwise SelectableFileRowViews added to a
        // detached parent render blank.
        _placeholder.Value = null;
        _unstaged.Value = snap.Unstaged;
        _staged.Value = ComputeDisplayedStaged();
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
                _opError.Value = errorMsg;
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
                _opError.Value = errorMsg;
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

    private void OnAmendToggled()
    {
        if (Amend.Value)
        {
            _preAmendTitle = Title.Value;
            _preAmendDescription = Description.Value;

            string title = string.Empty;
            string description = string.Empty;
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
            Title.Value = title;
            Description.Value = description;
        }
        else
        {
            Title.Value = _preAmendTitle;
            Description.Value = _preAmendDescription;
            _preAmendTitle = string.Empty;
            _preAmendDescription = string.Empty;
            _headFiles = null;
        }

        // Amend visibility flipped — re-render the staged panel so HEAD files appear
        // or disappear without waiting for the next snapshot reload.
        _staged.Value = ComputeDisplayedStaged();
    }

    private static bool HasNonWhitespace(string s)
    {
        foreach (var ch in s)
            if (!char.IsWhiteSpace(ch)) return true;
        return false;
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
