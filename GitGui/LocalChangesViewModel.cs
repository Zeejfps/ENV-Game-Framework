using System.Diagnostics;
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
    public IReadable<IReadOnlyList<SubmoduleInfo>> DriftedSubmodules { get; }
    public IReadable<Selection> Selection { get; }
    public IReadable<DiffTarget?> SelectedTarget { get; }
    public IReadable<bool> DiscardEnabled { get; }
    public IReadable<bool> StageSelectedEnabled { get; }
    public IReadable<string?> OpError { get; }
    public IReadable<bool> CommitEnabled { get; }
    public IReadable<bool> CommitBusy { get; }
    public IReadable<float> CommitRotation => _commitSpinner.Rotation;
    public DiffViewModel DiffVm { get; }

    private readonly SpinnerAnimation _commitSpinner;


    private IReadOnlyList<FileChange> _stagedFromIndex = Empty;
    private AmendSession? _amend;
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
        DriftedSubmodules = Slice(s => s.DriftedSubmodules);
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
        DiffVm = new DiffViewModel(SelectedTarget, registry, gitService, dispatcher, bus);

        Subscriptions.Add(_registry.Active.Subscribe(_ => StartLoadForActiveRepo()));
        Subscriptions.Add(_bus.SubscribeScoped<WorkingTreeChangedMessage>(OnWorkingTreeChanged));
        Subscriptions.Add(_bus.SubscribeScoped<SubmodulesChangedMessage>(OnSubmodulesChanged));
        Subscriptions.Add(_bus.SubscribeScoped<HunkAppliedOptimisticMessage>(OnHunkAppliedOptimistic));
    }

    private void OnHunkAppliedOptimistic(HunkAppliedOptimisticMessage msg)
    {
        var active = _registry.Active.Value;
        if (active == null || active.Id != msg.RepoId) return;

        Update(s =>
        {
            var unstaged = s.Unstaged;
            var staged = s.Staged;

            FileChange? entry = msg.FromSide == DiffSide.Unstaged
                ? FindByPath(unstaged, msg.Path)
                : FindByPath(staged, msg.Path);
            if (entry == null) return s;

            if (msg.IsLastHunk)
            {
                if (msg.FromSide == DiffSide.Unstaged)
                    unstaged = RemoveByPath(unstaged, msg.Path);
                else if (msg.FromSide == DiffSide.Staged)
                    staged = RemoveByPath(staged, msg.Path);
            }

            if (msg.ToSide is DiffSide to)
            {
                if (to == DiffSide.Unstaged && FindByPath(unstaged, msg.Path) == null)
                    unstaged = InsertSorted(unstaged, entry);
                else if (to == DiffSide.Staged && FindByPath(staged, msg.Path) == null)
                    staged = InsertSorted(staged, entry);
            }

            // When the file fully moves to the other side, keep the user's focus on it by
            // shifting the selection to the destination side — same behavior as the
            // full-file stage/unstage flow in RunIndexOp.
            Selection selection;
            if (msg.IsLastHunk && msg.ToSide is DiffSide moved)
                selection = GitGui.Selection.FromPaths(new[] { msg.Path }, moved, unstaged, staged);
            else
                selection = GitGui.Selection.Create(s.Selection.Items, s.Selection.Anchor, s.Selection.Cursor, unstaged, staged);

            return s with { Unstaged = unstaged, Staged = staged, Selection = selection };
        });
    }

    private static FileChange? FindByPath(IReadOnlyList<FileChange> list, string path)
    {
        foreach (var f in list) if (f.Path == path) return f;
        return null;
    }

    private static IReadOnlyList<FileChange> RemoveByPath(IReadOnlyList<FileChange> list, string path)
    {
        var next = new List<FileChange>(list.Count);
        foreach (var f in list) if (f.Path != path) next.Add(f);
        return next;
    }

    private static IReadOnlyList<FileChange> InsertSorted(IReadOnlyList<FileChange> list, FileChange entry)
    {
        var next = new List<FileChange>(list.Count + 1);
        next.AddRange(list);
        next.Add(entry);
        next.Sort(static (a, b) => string.Compare(a.Path, b.Path, StringComparison.OrdinalIgnoreCase));
        return next;
    }

    private void OnSubmodulesChanged(SubmodulesChangedMessage msg)
    {
        var active = _registry.Active.Value;
        if (active is null) return;
        var primaryId = active.IsPrimary ? active.Id : (active.ParentRepoId ?? active.Id);
        if (primaryId != msg.PrimaryRepoId) return;
        StartLoadForActiveRepo();
    }

    public override void Dispose()
    {
        DiffVm.Dispose();
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
                Selection = GitGui.Selection.Create(s.Selection.Items, s.Selection.Anchor, s.Selection.Cursor, s.Unstaged, staged),
            };
        });
    }
    
    /// <summary>
    /// Updates selection for a row click. Plain click replaces the selection with the
    /// clicked target; Ctrl/Cmd toggles it; Shift extends the range from the anchor
    /// (same side only). The anchor moves on plain/toggle clicks and stays put on
    /// shift-extends so subsequent shift-clicks pivot around it.
    /// </summary>
    public void SelectRow(string path, DiffSide side, InputModifiers modifiers)
    {
        var shift = (modifiers & InputModifiers.Shift) != 0;
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
                        Selection = GitGui.Selection.Create(range, sel.Anchor, clicked, s.Unstaged, s.Staged),
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
                    Selection = GitGui.Selection.Create(next, clicked, clicked, s.Unstaged, s.Staged),
                };
            }

            return s with
            {
                Selection = GitGui.Selection.Create([clicked], clicked, clicked, s.Unstaged, s.Staged),
            };
        });
    }

    /// <summary>
    /// Moves the keyboard cursor up or down within the currently active side, clamping
    /// at the list edges. With no selection, the first press lands on the top or bottom
    /// row of the unstaged side (or the staged side if unstaged is empty). When
    /// <paramref name="extend"/> is true the anchor stays put and the range grows or
    /// shrinks toward the new cursor position; otherwise the selection collapses to
    /// the single new row.
    /// </summary>
    public void MoveSelection(int delta, bool extend)
    {
        if (delta == 0) return;

        Update(s =>
        {
            DiffSide side;
            IReadOnlyList<FileChange> sideFiles;
            if (s.Selection.Count > 0)
            {
                side = s.Selection.Items[0].Side;
                sideFiles = side == DiffSide.Unstaged ? s.Unstaged : s.Staged;
            }
            else if (s.Unstaged.Count > 0)
            {
                side = DiffSide.Unstaged;
                sideFiles = s.Unstaged;
            }
            else if (s.Staged.Count > 0)
            {
                side = DiffSide.Staged;
                sideFiles = s.Staged;
            }
            else
            {
                return s;
            }

            if (sideFiles.Count == 0) return s;

            int currentIdx;
            if (s.Selection.Cursor != null && s.Selection.Cursor.Side == side)
                currentIdx = IndexOfPath(sideFiles, s.Selection.Cursor.Path);
            else if (s.Selection.Count > 0)
                currentIdx = IndexOfPath(sideFiles, s.Selection.Items[^1].Path);
            else
                currentIdx = delta > 0 ? -1 : sideFiles.Count;

            var newIdx = Math.Clamp(currentIdx + delta, 0, sideFiles.Count - 1);
            if (newIdx == currentIdx && s.Selection.Count > 0 && !extend) return s;

            var target = new DiffTarget(sideFiles[newIdx].Path, side);

            if (extend && s.Selection.Anchor != null && s.Selection.Anchor.Side == side)
            {
                var anchorIdx = IndexOfPath(sideFiles, s.Selection.Anchor.Path);
                if (anchorIdx >= 0)
                {
                    var lo = Math.Min(anchorIdx, newIdx);
                    var hi = Math.Max(anchorIdx, newIdx);
                    var range = new List<DiffTarget>(hi - lo + 1);
                    for (var i = lo; i <= hi; i++)
                        range.Add(new DiffTarget(sideFiles[i].Path, side));
                    return s with
                    {
                        Selection = GitGui.Selection.Create(range, s.Selection.Anchor, target, s.Unstaged, s.Staged),
                    };
                }
            }

            return s with
            {
                Selection = GitGui.Selection.Create([target], target, target, s.Unstaged, s.Staged),
            };
        });
    }

    public void ClearSelection()
    {
        if (State.Value.Selection.Count == 0 && State.Value.Selection.Anchor == null) return;
        Update(s => s with { Selection = GitGui.Selection.Empty });
    }
    
    public void Stage(IReadOnlyList<string> paths) => RunIndexOp(paths, isStage: true);
    
    public void StageSubmodulePointer(string submodulePath) => RunIndexOp([submodulePath], isStage: true);

    // Resets a submodule's working tree back to the SHA the parent has recorded. Runs
    // `git submodule update -- <path>` and broadcasts SubmodulesChangedMessage so the
    // drift list refreshes once the watcher / re-load catches up.
    public void ResetSubmoduleToRecorded(string submodulePath)
    {
        var repo = _registry.Active.Value;
        if (repo == null) return;
        var req = new SubmoduleUpdateRequest(
            Paths: [submodulePath],
            Init: false,
            Recursive: false,
            Mode: SubmoduleUpdateMode.Checkout);
        var primaryId = repo.IsPrimary ? repo.Id : (repo.ParentRepoId ?? repo.Id);
        RunBackground<SubmoduleUpdateOutcome>(
            work: () =>
            {
                try { return (_gitService.UpdateSubmodules(repo, req), null); }
                catch (Exception ex) { return (null, ex.Message); }
            },
            onResult: (outcome, errorMsg) =>
            {
                if (errorMsg != null) { Update(s => s with { OpError = errorMsg }); return; }
                if (outcome is { Success: false }) { Update(s => s with { OpError = outcome.ErrorMessage }); return; }
                _bus.Broadcast(new SubmodulesChangedMessage(primaryId));
            });
    }

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
        _bus.Broadcast(new ShowDialogMessage(onClose => new DiscardChangesDialog(repo, paths, onClose)));
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
        // Drift state is fetched from the same place — submodules can drift independently
        // of file changes (a sibling terminal can move a submodule's HEAD without touching
        // the parent's working tree), so we always re-query alongside the snapshot.
        // Submodules themselves don't have nested submodules in our model (one level deep),
        // so skip the query when the active row is itself a submodule.
        var canQuerySubmodules = !repo.IsSubmodule;
        var loadTag = $"[LocalChanges load {repo.Path}]";
        Console.WriteLine($"{loadTag} starting (amending={amending}, querySubmodules={canQuerySubmodules})");
        var loadSw = Stopwatch.StartNew();
        RunBackground<LoadResult>(
            work: () =>
            {
                var snap = _gitService.GetLocalChanges(repo);
                if (snap.ErrorMessage != null) return (null, snap.ErrorMessage);
                var headFiles = amending ? _gitService.GetHeadCommitFiles(repo) : null;
                IReadOnlyList<SubmoduleInfo> drift = Array.Empty<SubmoduleInfo>();
                if (canQuerySubmodules)
                {
                    var subs = _gitService.ListSubmodules(repo, out _);
                    if (subs.Count > 0)
                    {
                        var driftList = new List<SubmoduleInfo>();
                        foreach (var s in subs)
                        {
                            if (s.Status == SubmoduleStatus.UpToDate) continue;
                            if (s.Status == SubmoduleStatus.Modified) continue;
                            driftList.Add(s);
                        }
                        drift = driftList;
                    }
                }
                return (new LoadResult(snap, headFiles, drift), null);
            },
            onResult: (result, errorMsg) =>
            {
                loadSw.Stop();
                Console.WriteLine($"{loadTag} finished in {loadSw.ElapsedMilliseconds}ms (error={errorMsg ?? "none"})");
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
                        DriftedSubmodules = [],
                    });
                    return;
                }
                if (result == null) return;
                if (_amend != null && result.HeadFiles != null)
                    _amend.UpdateHeadFiles(result.HeadFiles);
                ApplySnapshot(result.Snap, result.Drift);
            });
    }

    private sealed record LoadResult(
        LocalChangesSnapshot Snap,
        IReadOnlyList<FileChange>? HeadFiles,
        IReadOnlyList<SubmoduleInfo> Drift);

    // Writes a fresh snapshot — new lists plus whatever selection the caller computes
    // from them — through a single atomic Update. The selection callback receives the
    // pre-update state (so reload-style callers can carry the prior selection through
    // Selection.Create normalization) and the newly-computed displayed staged (so all
    // callers see the same amend-aware view of the staged side the state is about to
    // hold). When drift is null, the existing DriftedSubmodules stays put — used by
    // stage/unstage/commit, which only mutate the file lists.
    private void ApplySnapshot(
        LocalChangesSnapshot snap,
        Func<LocalChangesState, IReadOnlyList<FileChange>, Selection> selectionFor,
        IReadOnlyList<SubmoduleInfo>? drift = null)
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
                DriftedSubmodules = drift ?? s.DriftedSubmodules,
            };
        });
    }

    // Reload-style apply: keep the existing selection (paths still in the lists survive,
    // gone paths are pruned by Selection.Create). Used for cross-repo reloads, watcher
    // ticks, refs changes, and post-commit snapshots — anywhere the lists change but the
    // selection isn't being explicitly steered to a new place.
    private void ApplySnapshot(LocalChangesSnapshot snap, IReadOnlyList<SubmoduleInfo>? drift = null)
        => ApplySnapshot(snap, (s, staged) =>
            GitGui.Selection.Create(s.Selection.Items, s.Selection.Anchor, s.Selection.Cursor, snap.Unstaged, staged), drift);

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
