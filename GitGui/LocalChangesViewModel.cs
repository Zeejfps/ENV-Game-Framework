using ZGF.Gui;
using ZGF.Observable;

namespace GitGui;

/// <summary>
/// View model for the Local Changes feature. State lives in a single immutable
/// <see cref="LocalChangesState"/> record; <see cref="ViewModelBase{TState}.Update"/> is
/// the only mutation primitive. Views subscribe to per-field slices (auto-deduped by
/// equality) and call the command methods to drive git ops and row interactions.
///
/// Selection state lives here too — not in the panels — so the lists and the selection
/// always change in lockstep through a single <see cref="Update"/> call. That makes
/// invalid combinations (a selected path no longer in any list, the diff view targeting
/// a path that just moved sides) unrepresentable, and removes the cross-panel
/// coordination that used to be needed to keep the two sides mutually exclusive.
/// </summary>
internal sealed class LocalChangesViewModel : ViewModelBase<LocalChangesState>
{
    private static readonly IReadOnlyList<FileChange> Empty = [];

    private readonly IRepoRegistry _registry;
    private readonly IGitService _gitService;
    private readonly IMessageBus _bus;

    public IReadable<string> Title { get; }
    public IReadable<string> Description { get; }
    public IReadable<bool> Amend { get; }
    public IReadable<string?> Placeholder { get; }
    public IReadable<IReadOnlyList<FileChange>> Unstaged { get; }
    public IReadable<IReadOnlyList<FileChange>> Staged { get; }
    public IReadable<Selection> Selection { get; }
    public IReadable<DiffTarget?> SelectedTarget { get; }
    public IReadable<bool> DiscardEnabled { get; }
    public IReadable<bool> StageSelectedEnabled { get; }
    public IReadable<string?> OpError { get; }
    public IReadable<bool> CommitEnabled { get; }
    public IReadable<bool> CommitBusy { get; }
    public IReadable<float> CommitRotation => _commitSpinner.Rotation;

    private readonly SpinnerAnimation _commitSpinner;

    // _stagedFromIndex is whatever GetLocalChanges last returned; the amend-only
    // bookkeeping (HEAD files, pre-amend editor backups) lives on _amend, which is
    // non-null exactly when the user is amending.
    private IReadOnlyList<FileChange> _stagedFromIndex = Empty;
    private AmendSession? _amend;

    // Tracks which repo we last loaded so cross-repo switches can clear stale lists,
    // while same-repo reloads (watcher tick, refs change) keep the panels visible
    // during the refetch.
    private Guid? _lastLoadedRepoId;

    public LocalChangesViewModel(
        IRepoRegistry registry,
        IGitService gitService,
        IUiDispatcher dispatcher,
        IMessageBus bus)
        : base(dispatcher, LocalChangesState.Initial)
    {
        _registry = registry;
        _gitService = gitService;
        _bus = bus;

        Title = Slice(s => s.Title);
        Description = Slice(s => s.Description);
        Amend = Slice(s => s.Amend);
        Placeholder = Slice(s => s.Placeholder);
        Unstaged = Slice(s => s.Unstaged);
        Staged = Slice(s => s.Staged);
        Selection = Slice(s => s.Selection);
        SelectedTarget = Slice(s => s.Selection.Single);
        DiscardEnabled = Slice(s =>
            s.Selection.Count > 0 && s.Selection.Items[0].Side == DiffSide.Unstaged);
        StageSelectedEnabled = Slice(s =>
            s.Selection.Count > 0 && s.Selection.Items[0].Side == DiffSide.Unstaged);
        OpError = Slice(s => s.OpError);
        CommitEnabled = Slice(s => s.CommitEnabled);
        CommitBusy = Slice(s => s.CommitBusy);

        _commitSpinner = new SpinnerAnimation(dispatcher);

        Subscriptions.Add(_registry.Active.Subscribe(_ => StartLoadForActiveRepo()));
        Subscriptions.Add(_bus.SubscribeScoped<WorkingTreeChangedMessage>(OnWorkingTreeChanged));
    }

    public override void Dispose()
    {
        _commitSpinner.Dispose();
        base.Dispose();
    }

    public void SetTitle(string value)
        => Update(s => s with { Title = value });

    public void SetDescription(string value)
        => Update(s => s with { Description = value });

    public void SetAmend(bool on)
    {
        string title, description;
        if (on)
        {
            _amend = AmendSession.Begin(
                _gitService,
                _registry.Active.Value,
                State.Value.Title,
                State.Value.Description);
            title = _amend.Title;
            description = _amend.Description;
        }
        else
        {
            title = _amend?.PreAmendTitle ?? string.Empty;
            description = _amend?.PreAmendDescription ?? string.Empty;
            _amend = null;
        }

        Update(s =>
        {
            var staged = ComputeDisplayedStaged();
            return s with
            {
                Amend = on,
                Title = title,
                Description = description,
                Staged = staged,
                Selection = GitGui.Selection.Create(s.Selection.Items, s.Selection.Anchor, s.Unstaged, staged),
            };
        });
    }

    // ------- row interactions (called by the view in response to row clicks) -------

    /// <summary>
    /// Updates selection for a row click. Plain click replaces the selection with the
    /// clicked target; Ctrl/Cmd toggles it; Shift extends the range from the anchor
    /// (same side only). The anchor moves on plain/toggle clicks and stays put on
    /// shift-extends so subsequent shift-clicks pivot around it.
    /// </summary>
    public void SelectRow(string path, DiffSide side, InputModifiers modifiers)
    {
        var shift = (modifiers & InputModifiers.Shift) != 0;
        // Cmd on macOS reports as Super; Ctrl on Windows/Linux as Control. Treat both
        // as the toggle-modifier so the panel feels right on every host.
        var toggle = (modifiers & (InputModifiers.Control | InputModifiers.Super)) != 0;
        var clicked = new DiffTarget(path, side);

        Update(s =>
        {
            var sel = s.Selection;
            var sideFiles = side == DiffSide.Unstaged ? s.Unstaged : s.Staged;

            if (shift && sel.Anchor != null && sel.Anchor.Side == side)
            {
                var anchorIdx = IndexOfPath(sideFiles, sel.Anchor.Path);
                var clickIdx = IndexOfPath(sideFiles, path);
                if (anchorIdx >= 0 && clickIdx >= 0)
                {
                    var lo = Math.Min(anchorIdx, clickIdx);
                    var hi = Math.Max(anchorIdx, clickIdx);
                    var range = new List<DiffTarget>(hi - lo + 1);
                    for (var i = lo; i <= hi; i++)
                        range.Add(new DiffTarget(sideFiles[i].Path, side));
                    return s with
                    {
                        Selection = GitGui.Selection.Create(range, sel.Anchor, s.Unstaged, s.Staged),
                    };
                }
            }

            if (toggle && sel.Count > 0 && sel.Items[0].Side == side)
            {
                var alreadySelected = sel.Contains(path, side);
                var next = sel.Items.Where(t => !t.Equals(clicked)).ToList();
                if (!alreadySelected) next.Add(clicked);
                return s with
                {
                    Selection = GitGui.Selection.Create(next, clicked, s.Unstaged, s.Staged),
                };
            }

            return s with
            {
                Selection = GitGui.Selection.Create(new[] { clicked }, clicked, s.Unstaged, s.Staged),
            };
        });
    }

    public void ClearSelection()
    {
        if (State.Value.Selection.Count == 0 && State.Value.Selection.Anchor == null) return;
        Update(s => s with { Selection = GitGui.Selection.Empty });
    }

    // ------- git ops -------

    public void Stage(IReadOnlyList<string> paths) => RunIndexOp(paths, isStage: true);

    public void Unstage(IReadOnlyList<string> paths)
    {
        if (paths.Count == 0) return;

        // While amending, the staged panel may include HEAD-only files (not in the
        // index) that the user wants to drop from the amended commit. Those need a
        // reset against HEAD~1; truly-staged files take the normal unstage path.
        if (_amend != null && _amend.HeadFiles.Count > 0)
        {
            var (toUnstage, toResetToParent) = _amend.Classify(paths, _stagedFromIndex);
            if (toResetToParent.Count > 0)
            {
                RunUnstageWithReset(toUnstage, toResetToParent);
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
        if (State.Value.CommitBusy) return;

        // CommitEnabled gates the button on a non-empty title (and, unless amending, at
        // least one staged file), so reaching this point implies the inputs are valid.
        var snapshot = State.Value;
        var title = snapshot.Title.Trim();
        var description = snapshot.Description.Trim();
        // Standard git format: subject, blank line, body. Skip the blank line when there's
        // no body so the message is just the subject.
        var message = description.Length > 0 ? $"{title}\n\n{description}" : title;
        var amend = snapshot.Amend;

        Update(s => s with { CommitBusy = true, OpError = null });
        _commitSpinner.Start();

        RunBackground<LocalChangesSnapshot>(
            work: () =>
            {
                var err = _gitService.Commit(repo, message, amend);
                if (err != null) return (null, err);
                var snap = _gitService.GetLocalChanges(repo);
                return snap.ErrorMessage != null ? (null, snap.ErrorMessage) : (snap, null);
            },
            onResult: (snap, errorMsg) =>
            {
                _commitSpinner.Stop();
                Update(s => s with { CommitBusy = false, OpError = errorMsg });
                if (errorMsg != null) return;

                // After a successful commit the editor is cleared regardless of mode.
                // When amending we also drop the session — bypassing SetAmend(false)'s
                // restore-from-backup, which would put the pre-amend text back.
                if (_amend != null)
                {
                    _amend = null;
                    Update(s => s with { Amend = false, Title = string.Empty, Description = string.Empty });
                }
                else
                {
                    Update(s => s with { Title = string.Empty, Description = string.Empty });
                }
                if (snap != null) ApplySnapshot(snap);
                _bus.Broadcast(new CommitCreatedMessage(repo.Id));
            });
    }

    private void OnWorkingTreeChanged(WorkingTreeChangedMessage msg)
    {
        var active = _registry.Active.Value;
        if (active == null || active.Id != msg.RepoId) return;
        StartLoadForActiveRepo();
    }

    private void StartLoadForActiveRepo()
    {
        var active = _registry.Active.Value;

        if (active == null)
        {
            // Bump to invalidate any in-flight load from the previous repo.
            Gen.Bump();
            _stagedFromIndex = Empty;
            _lastLoadedRepoId = null;
            Update(s => s with
            {
                HasRepo = false,
                IsLoading = false,
                LoadError = null,
                OpError = null,
                Staged = Empty,
                Unstaged = Empty,
                Selection = GitGui.Selection.Empty,
            });
            return;
        }

        // Cross-repo switches blank the panels (and the selection) so the "Loading…"
        // placeholder is shown rather than a stale snapshot from the previous repo.
        // Same-repo reloads (WorkingTreeChangedMessage) keep the lists visible so the
        // panels don't tear down for an incremental refresh.
        var isCrossRepoSwitch = _lastLoadedRepoId != active.Id;
        _lastLoadedRepoId = active.Id;

        Update(s => s with
        {
            HasRepo = true,
            IsLoading = true,
            LoadError = null,
            OpError = null,
            Staged = isCrossRepoSwitch ? Empty : s.Staged,
            Unstaged = isCrossRepoSwitch ? Empty : s.Unstaged,
            Selection = isCrossRepoSwitch ? GitGui.Selection.Empty : s.Selection,
        });

        var repo = active;
        // HEAD can move while amending (refs-changed reload, branch op elsewhere), so
        // refresh HEAD's file list alongside the index snapshot — otherwise the staged
        // panel keeps showing HEAD-only rows from a HEAD that no longer exists.
        var amending = _amend != null;
        RunBackground<LoadResult>(
            work: () =>
            {
                var snap = _gitService.GetLocalChanges(repo);
                if (snap.ErrorMessage != null) return (null, snap.ErrorMessage);
                var headFiles = amending ? _gitService.GetHeadCommitFiles(repo) : null;
                return (new LoadResult(snap, headFiles), null);
            },
            onResult: (result, errorMsg) =>
            {
                if (errorMsg != null)
                {
                    _stagedFromIndex = Empty;
                    Update(s => s with
                    {
                        IsLoading = false,
                        LoadError = errorMsg,
                        Staged = Empty,
                        Unstaged = Empty,
                        Selection = GitGui.Selection.Empty,
                    });
                    return;
                }
                if (result == null) return;
                if (_amend != null && result.HeadFiles != null)
                    _amend.UpdateHeadFiles(result.HeadFiles);
                ApplySnapshot(result.Snap);
            });
    }

    private sealed record LoadResult(LocalChangesSnapshot Snap, IReadOnlyList<FileChange>? HeadFiles);

    // Writes a fresh snapshot — new lists plus whatever selection the caller computes
    // from them — through a single atomic Update. The selection callback receives the
    // pre-update state (so reload-style callers can carry the prior selection through
    // Selection.Create normalization) and the newly-computed displayed staged (so all
    // callers see the same amend-aware view of the staged side the state is about to
    // hold).
    private void ApplySnapshot(LocalChangesSnapshot snap, Func<LocalChangesState, IReadOnlyList<FileChange>, Selection> selectionFor)
    {
        _stagedFromIndex = snap.Staged;
        Update(s =>
        {
            var staged = ComputeDisplayedStaged();
            return s with
            {
                IsLoading = false,
                LoadError = null,
                Unstaged = snap.Unstaged,
                Staged = staged,
                Selection = selectionFor(s, staged),
            };
        });
    }

    // Reload-style apply: keep the existing selection (paths still in the lists survive,
    // gone paths are pruned by Selection.Create). Used for cross-repo reloads, watcher
    // ticks, refs changes, and post-commit snapshots — anywhere the lists change but the
    // selection isn't being explicitly steered to a new place.
    private void ApplySnapshot(LocalChangesSnapshot snap)
        => ApplySnapshot(snap, (s, staged) =>
            GitGui.Selection.Create(s.Selection.Items, s.Selection.Anchor, snap.Unstaged, staged));

    private void RunIndexOp(IReadOnlyList<string> paths, bool isStage)
    {
        if (paths.Count == 0) return;
        var repo = _registry.Active.Value;
        if (repo == null) return;

        RunBackground<LocalChangesSnapshot>(
            work: () =>
            {
                if (isStage) _gitService.Stage(repo, paths);
                else _gitService.Unstage(repo, paths);
                var snap = _gitService.GetLocalChanges(repo);
                return snap.ErrorMessage != null ? (null, snap.ErrorMessage) : (snap, null);
            },
            onResult: (snap, errorMsg) =>
            {
                Update(s => s with { OpError = errorMsg });
                // Keep the prior snapshot rendered on failure — losing the list on every
                // transient error would erase the user's selection and context.
                if (snap == null) return;

                // Selection lands on the destination side at the just-moved paths.
                // FromPaths drops anything libgit2 didn't materialize in the new list.
                var destSide = isStage ? DiffSide.Staged : DiffSide.Unstaged;
                ApplySnapshot(snap, (_, staged) =>
                    GitGui.Selection.FromPaths(paths, destSide, snap.Unstaged, staged));
            });
    }

    private void RunUnstageWithReset(IReadOnlyList<string> toUnstage, IReadOnlyList<string> toResetToParent)
    {
        var repo = _registry.Active.Value;
        if (repo == null) return;

        RunBackground<LocalChangesSnapshot>(
            work: () =>
            {
                if (toUnstage.Count > 0) _gitService.Unstage(repo, toUnstage);
                if (toResetToParent.Count > 0) _gitService.ResetToParent(repo, toResetToParent);
                var snap = _gitService.GetLocalChanges(repo);
                return snap.ErrorMessage != null ? (null, snap.ErrorMessage) : (snap, null);
            },
            onResult: (snap, errorMsg) =>
            {
                Update(s => s with { OpError = errorMsg });
                if (snap == null) return;

                // Both batches land on the unstaged side after the reset/unstage.
                var movedToUnstaged = new List<string>(toUnstage.Count + toResetToParent.Count);
                movedToUnstaged.AddRange(toUnstage);
                movedToUnstaged.AddRange(toResetToParent);
                ApplySnapshot(snap, (_, staged) =>
                    GitGui.Selection.FromPaths(movedToUnstaged, DiffSide.Unstaged, snap.Unstaged, staged));
            });
    }

    private static int IndexOfPath(IReadOnlyList<FileChange> files, string path)
    {
        for (var i = 0; i < files.Count; i++)
            if (files[i].Path == path) return i;
        return -1;
    }

    private IReadOnlyList<FileChange> ComputeDisplayedStaged()
        => _amend?.MergeWithIndex(_stagedFromIndex) ?? _stagedFromIndex;
}
