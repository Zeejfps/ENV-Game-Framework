using ZGF.Observable;

namespace GitGui;

internal sealed class StashPresenter : IDisposable
{
    private readonly IStashView _view;
    private readonly StashRequest _request;
    private readonly IGitService _gitService;
    private readonly IMessageBus _bus;
    private readonly OperationRunner _runner;
    private readonly HashSet<string> _untrackedPaths = new();

    public StashPresenter(
        IStashView view,
        StashRequest request,
        IGitService gitService,
        IUiDispatcher dispatcher,
        IMessageBus bus,
        LocalChangesSelectionStore selectionStore)
    {
        _view = view;
        _request = request;
        _gitService = gitService;
        _bus = bus;
        _runner = new OperationRunner(dispatcher);

        var snapshot = _gitService.GetLocalChanges(_request.Repo);
        var rows = BuildRows(snapshot, _untrackedPaths);
        var preChecked = ComputePreChecked(rows, selectionStore.UnstagedPaths.Value);

        _view.MessageChanged += OnReadinessChanged;
        _view.SelectionChanged += OnReadinessChanged;
        _view.StashRequested += TryStash;
        _view.StashEnabled = false;
        _view.SetFiles(rows, preChecked);
        _view.FocusMessage();
    }

    public void Dispose()
    {
        _view.MessageChanged -= OnReadinessChanged;
        _view.SelectionChanged -= OnReadinessChanged;
        _view.StashRequested -= TryStash;
    }

    private void OnReadinessChanged()
    {
        if (_runner.IsRunning) return;
        _view.StashEnabled = _view.Message.Length > 0 && _view.SelectedPaths.Count > 0;
    }

    private void TryStash()
    {
        if (_runner.IsRunning) return;
        var message = _view.Message;
        if (message.Length == 0) return;
        var paths = _view.SelectedPaths;
        if (paths.Count == 0) return;

        var includeUntracked = false;
        foreach (var p in paths)
        {
            if (_untrackedPaths.Contains(p)) { includeUntracked = true; break; }
        }
        var keepIndex = _view.KeepStaged;
        var repoId = _request.Repo.Id;

        _view.StashEnabled = false;
        _view.ErrorMessage = null;

        _runner.Run(
            () => _gitService.CreateStash(_request.Repo, message, includeUntracked, keepIndex, paths),
            ex => new StashOutcome(false, ex.Message),
            outcome =>
            {
                if (!outcome.Success)
                {
                    _view.ErrorMessage = outcome.ErrorMessage ?? "Stash failed.";
                    OnReadinessChanged();
                    return;
                }
                _view.Close();
                _bus.Broadcast(new RefsChangedMessage(repoId));
                _bus.Broadcast(new WorkingTreeChangedMessage(repoId));
            });
    }

    private static IReadOnlyList<StashFileRow> BuildRows(LocalChangesSnapshot snapshot, HashSet<string> untracked)
    {
        untracked.Clear();
        var seen = new Dictionary<string, StashFileRow>(snapshot.Staged.Count + snapshot.Unstaged.Count);
        // Unstaged first so the worktree status wins the display when a path appears on both sides.
        foreach (var f in snapshot.Unstaged)
        {
            var isUntracked = f.Status == FileChangeStatus.Added;
            if (isUntracked) untracked.Add(f.Path);
            seen[f.Path] = new StashFileRow(f.Path, f, isUntracked);
        }
        foreach (var f in snapshot.Staged)
        {
            if (!seen.ContainsKey(f.Path))
                seen[f.Path] = new StashFileRow(f.Path, f, false);
        }
        var rows = seen.Values.ToList();
        rows.Sort(static (a, b) => string.Compare(a.Path, b.Path, StringComparison.OrdinalIgnoreCase));
        return rows;
    }

    private static IReadOnlyList<string> ComputePreChecked(IReadOnlyList<StashFileRow> rows, IReadOnlyList<string> unstagedSelection)
    {
        if (unstagedSelection.Count == 0)
            return rows.Select(r => r.Path).ToList();

        var selSet = new HashSet<string>(unstagedSelection);
        var preChecked = new List<string>(unstagedSelection.Count);
        foreach (var r in rows)
            if (selSet.Contains(r.Path)) preChecked.Add(r.Path);
        return preChecked;
    }
}
