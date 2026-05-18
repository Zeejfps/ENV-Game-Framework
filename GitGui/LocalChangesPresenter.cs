using ZGF.Observable;

namespace GitGui;

internal sealed class LocalChangesPresenter : IDisposable
{
    private const string OpenRepoPlaceholder = "Open a repository to see local changes.";
    private const string LoadingPlaceholder = "Loading…";

    private readonly ILocalChangesView _view;
    private readonly IRepoRegistry _registry;
    private readonly IGitService _gitService;
    private readonly IUiDispatcher _dispatcher;
    private readonly IMessageBus _bus;
    private readonly SubscriptionGroup _subscriptions = new();
    private readonly GenerationGuard _loadGen = new();

    // Cached so OnAmendToggled can re-derive the displayed staged list without
    // touching git. _stagedFromIndex is whatever GetLocalChanges last returned;
    // _headFiles is the diff of HEAD vs HEAD~1, populated only while amending.
    private IReadOnlyList<FileChange> _stagedFromIndex = Array.Empty<FileChange>();
    private IReadOnlyList<FileChange>? _headFiles;
    private string _preAmendTitle = string.Empty;
    private string _preAmendDescription = string.Empty;
    private int _displayedStagedCount;

    public LocalChangesPresenter(
        ILocalChangesView view,
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

        _view.StageRequested += OnStageRequested;
        _view.UnstageRequested += OnUnstageRequested;
        _view.TitleChanged += OnTitleChanged;
        _view.AmendToggled += OnAmendToggled;
        _view.CommitClicked += OnCommitClicked;

        _view.ShowPlaceholder(OpenRepoPlaceholder);
        _view.CommitEnabled = false;

        _subscriptions.Add(_registry.Active.Subscribe(_ => StartLoadForActiveRepo()));
        _subscriptions.Add(_bus.SubscribeScoped<RefsChangedMessage>(OnRefsChanged));
    }

    public void Dispose()
    {
        _loadGen.Bump();
        _subscriptions.Dispose();
        _view.StageRequested -= OnStageRequested;
        _view.UnstageRequested -= OnUnstageRequested;
        _view.TitleChanged -= OnTitleChanged;
        _view.AmendToggled -= OnAmendToggled;
        _view.CommitClicked -= OnCommitClicked;
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
        _view.OpError = null;

        if (active == null)
        {
            _view.ShowPlaceholder(OpenRepoPlaceholder);
            _displayedStagedCount = 0;
            _view.CommitEnabled = false;
            return;
        }

        _view.ShowPlaceholder(LoadingPlaceholder);
        _view.CommitEnabled = false;

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
                    _view.ShowPlaceholder(errorMsg);
                    _displayedStagedCount = 0;
                    _view.CommitEnabled = false;
                    return;
                }
                if (snap != null) ApplySnapshot(snap);
            });
        });
    }

    private void ApplySnapshot(LocalChangesSnapshot snap)
    {
        _stagedFromIndex = snap.Staged;
        var displayed = ComputeDisplayedStaged();
        _displayedStagedCount = displayed.Count;
        _view.ShowSnapshot(snap.Unstaged, displayed);
        UpdateCommitEnabled();
    }

    private void OnStageRequested(IReadOnlyList<string> paths) => RunIndexOp(paths, isStage: true);

    private void OnUnstageRequested(IReadOnlyList<string> paths)
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
                _view.OpError = errorMsg;
                // Keep the prior snapshot rendered on failure — losing the list on every
                // transient error would erase the user's selection and context.
                if (newSnap != null)
                {
                    ApplySnapshot(newSnap);
                    // The operated-on rows just moved sides; re-select them on the
                    // destination so the user keeps their place across a stage/unstage.
                    if (isStage) _view.SelectStaged(paths);
                    else _view.SelectUnstaged(paths);
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
                _view.OpError = errorMsg;
                if (newSnap != null)
                {
                    ApplySnapshot(newSnap);
                    // Both batches land in unstaged after the reset/unstage.
                    var combined = new List<string>(toUnstage.Count + toResetToParent.Count);
                    combined.AddRange(toUnstage);
                    combined.AddRange(toResetToParent);
                    _view.SelectUnstaged(combined);
                }
            });
        });
    }

    private void OnCommitClicked()
    {
        var repo = _registry.Active.Value;
        if (repo == null) return;

        // UpdateCommitEnabled gates the button on a non-empty title (and, unless
        // amending, at least one staged file), so reaching this point implies the
        // inputs are valid for the current mode.
        var title = _view.TitleText.Trim();
        var description = _view.DescriptionText.Trim();
        // Standard git format: subject, blank line, body. Skip the blank line when there's
        // no body so the message is just the subject.
        var message = description.Length > 0 ? $"{title}\n\n{description}" : title;
        var amend = _view.AmendChecked;

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
                _view.OpError = errorMsg;
                if (errorMsg != null) return;

                // Reset the pre-amend snapshot so toggling amend back off after the commit
                // doesn't restore stale text from a different commit's session.
                _preAmendTitle = string.Empty;
                _preAmendDescription = string.Empty;
                if (_view.AmendChecked)
                {
                    // Flipping fires AmendToggled which clears the inputs (saved state
                    // was just emptied above).
                    _view.AmendChecked = false;
                }
                else
                {
                    _view.TitleText = string.Empty;
                    _view.DescriptionText = string.Empty;
                }
                if (newSnap != null) ApplySnapshot(newSnap);
                bus.Broadcast(new CommitCreatedMessage(repo.Id));
            });
        });
    }

    private void OnAmendToggled()
    {
        if (_view.AmendChecked)
        {
            _preAmendTitle = _view.TitleText;
            _preAmendDescription = _view.DescriptionText;

            string title = string.Empty;
            string description = string.Empty;
            IReadOnlyList<FileChange> headFiles = Array.Empty<FileChange>();
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
            _view.TitleText = title;
            _view.DescriptionText = description;
        }
        else
        {
            _view.TitleText = _preAmendTitle;
            _view.DescriptionText = _preAmendDescription;
            _preAmendTitle = string.Empty;
            _preAmendDescription = string.Empty;
            _headFiles = null;
        }

        // Amend visibility flipped — re-render the staged panel so HEAD files appear
        // or disappear without waiting for the next snapshot reload.
        var displayed = ComputeDisplayedStaged();
        _displayedStagedCount = displayed.Count;
        _view.SetStagedFiles(displayed);
        UpdateCommitEnabled();
    }

    private void OnTitleChanged() => UpdateCommitEnabled();

    private void UpdateCommitEnabled()
    {
        var hasTitle = HasNonWhitespace(_view.TitleText);
        // Amend can be a message-only edit of the previous commit, so it doesn't need
        // anything staged; a regular commit does.
        var amend = _view.AmendChecked;
        _view.CommitEnabled = hasTitle && (amend || _displayedStagedCount > 0);
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
