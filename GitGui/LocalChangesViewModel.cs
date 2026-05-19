using ZGF.Observable;

namespace GitGui;

/// <summary>
/// View model for the Local Changes feature. State lives in a single immutable
/// <see cref="LocalChangesState"/> record; <see cref="ViewModelBase{TState}.Update"/> is
/// the only mutation primitive. Views subscribe to per-field slices (auto-deduped by
/// equality) and call the command methods to drive git ops. The VM holds no view references.
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
    public IReadable<string?> OpError { get; }
    public IReadable<bool> CommitEnabled { get; }

    /// <summary>Fired after a successful stage/unstage so the view can re-select the
    /// affected rows on the destination side — the panel-internal selection state is
    /// owned by the view, not the VM.</summary>
    public event Action<DiffSide, IReadOnlyList<string>>? SelectionRequested;

    // _stagedFromIndex is whatever GetLocalChanges last returned; the amend-only
    // bookkeeping (HEAD files, pre-amend editor backups) lives on _amend, which is
    // non-null exactly when the user is amending.
    private IReadOnlyList<FileChange> _stagedFromIndex = Empty;
    private AmendSession? _amend;

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
        OpError = Slice(s => s.OpError);
        CommitEnabled = Slice(s => s.CommitEnabled);

        Subscriptions.Add(_registry.Active.Subscribe(_ => StartLoadForActiveRepo()));
        Subscriptions.Add(_bus.SubscribeScoped<RefsChangedMessage>(OnRefsChanged));
    }

    public void SetTitle(string value)
        => Update(s => s with { Title = value });

    public void SetDescription(string value)
        => Update(s => s with { Description = value });

    public void SetAmend(bool on)
    {
        if (on)
        {
            _amend = AmendSession.Begin(
                _gitService,
                _registry.Active.Value,
                State.Value.Title,
                State.Value.Description);

            Update(s => s with
            {
                Amend = true,
                Title = _amend.Title,
                Description = _amend.Description,
                Staged = ComputeDisplayedStaged(),
            });
        }
        else
        {
            var title = _amend?.PreAmendTitle ?? string.Empty;
            var description = _amend?.PreAmendDescription ?? string.Empty;
            _amend = null;

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

        // CommitEnabled gates the button on a non-empty title (and, unless amending, at
        // least one staged file), so reaching this point implies the inputs are valid.
        var snapshot = State.Value;
        var title = snapshot.Title.Trim();
        var description = snapshot.Description.Trim();
        // Standard git format: subject, blank line, body. Skip the blank line when there's
        // no body so the message is just the subject.
        var message = description.Length > 0 ? $"{title}\n\n{description}" : title;
        var amend = snapshot.Amend;

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
                Update(s => s with { OpError = errorMsg });
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

        if (active == null)
        {
            // Bump to invalidate any in-flight load from the previous repo.
            Gen.Bump();
            _stagedFromIndex = Empty;
            // Any error from a previous repo's op no longer applies once we switch.
            Update(s => s with
            {
                Placeholder = LocalChangesState.OpenRepoPlaceholder,
                OpError = null,
                Staged = Empty,
            });
            return;
        }

        Update(s => s with
        {
            Placeholder = LocalChangesState.LoadingPlaceholder,
            OpError = null,
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
                    Update(s => s with { Placeholder = errorMsg, Staged = Empty });
                    return;
                }
                if (result == null) return;
                if (_amend != null && result.HeadFiles != null)
                    _amend.UpdateHeadFiles(result.HeadFiles);
                ApplySnapshot(result.Snap);
            });
    }

    private sealed record LoadResult(LocalChangesSnapshot Snap, IReadOnlyList<FileChange>? HeadFiles);

    private void ApplySnapshot(LocalChangesSnapshot snap)
    {
        _stagedFromIndex = snap.Staged;
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
                if (snap != null)
                {
                    ApplySnapshot(snap);
                    // The operated-on rows just moved sides; re-select them on the
                    // destination so the user keeps their place across a stage/unstage.
                    SelectionRequested?.Invoke(isStage ? DiffSide.Staged : DiffSide.Unstaged, paths);
                }
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
                ApplySnapshot(snap);
                // Both batches land in unstaged after the reset/unstage.
                var combined = new List<string>(toUnstage.Count + toResetToParent.Count);
                combined.AddRange(toUnstage);
                combined.AddRange(toResetToParent);
                SelectionRequested?.Invoke(DiffSide.Unstaged, combined);
            });
    }

    private IReadOnlyList<FileChange> ComputeDisplayedStaged()
        => _amend?.MergeWithIndex(_stagedFromIndex) ?? _stagedFromIndex;
}