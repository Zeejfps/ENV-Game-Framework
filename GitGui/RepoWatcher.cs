using ZGF.Observable;

namespace GitGui;

// Watches a single repository's filesystem for changes the user makes outside the
// app (editor saves, terminal `git` commands, builds, IDE checkouts, …) and turns
// them into the same MessageBus signals the in-app presenters already use.
//
// Two watchers per repo:
//   * Working tree (root, recursive, excluding .git): edits → WorkingTreeChangedMessage.
//   * .git directory: HEAD/refs/packed-refs/FETCH_HEAD/ORIG_HEAD/MERGE_HEAD → RefsChangedMessage.
//
// FSW fires events on threadpool threads in storms (a single editor save can be 3-5
// events; a build or git checkout can be thousands), so we debounce per channel and
// post the final broadcast through IUiDispatcher onto the UI thread.
//
// Design note — feedback loop avoidance:
//   We intentionally do NOT call libgit2 inside the debounce callback (e.g. to hash
//   a status snapshot and suppress no-op broadcasts). libgit2's RetrieveStatus updates
//   `.git/index`'s stat cache as a side effect, which fires our own `.git` watcher, which
//   schedules another debounce, which calls libgit2 again — an infinite loop. We also do
//   NOT treat `.git/index` as a working-tree signal for the same reason: every read-side
//   status call by the VM would re-trigger our watcher.
//
//   The cost: saves to `.gitignored` paths produce a broadcast and a redundant VM
//   GetLocalChanges call, even though git's view didn't change. That's cheap because
//   LocalChangesViewModel keeps the panels mounted during refresh (see LocalChangesState's
//   derived Placeholder) and identical snapshots produce no visible repaint beyond the
//   row list re-bind.
internal sealed class RepoWatcher : IDisposable
{
    private const int DebounceMs = 250;
    private const int FswBufferBytes = 64 * 1024;

    private static readonly StringComparison PathCmp =
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    private readonly Repo _repo;
    private readonly IUiDispatcher _dispatcher;
    private readonly IMessageBus _bus;

    private readonly FileSystemWatcher? _treeWatcher;
    private readonly FileSystemWatcher? _gitWatcher;
    private readonly Timer _workingTreeDebounce;
    private readonly Timer _refsDebounce;

    private readonly string _gitDirPrefix;

    private int _disposed;

    public RepoWatcher(Repo repo, IUiDispatcher dispatcher, IMessageBus bus)
    {
        _repo = repo;
        _dispatcher = dispatcher;
        _bus = bus;
        _gitDirPrefix = Path.Combine(repo.Path, ".git") + Path.DirectorySeparatorChar;

        _workingTreeDebounce = new Timer(_ => OnWorkingTreeDebounce(), null, Timeout.Infinite, Timeout.Infinite);
        _refsDebounce = new Timer(_ => OnRefsDebounce(), null, Timeout.Infinite, Timeout.Infinite);

        _treeWatcher = TryCreateWatcher(repo.Path);
        if (_treeWatcher != null)
        {
            _treeWatcher.Created += OnTreeEvent;
            _treeWatcher.Changed += OnTreeEvent;
            _treeWatcher.Deleted += OnTreeEvent;
            _treeWatcher.Renamed += OnTreeRenamed;
            _treeWatcher.Error += OnError;
        }

        var gitDir = Path.Combine(repo.Path, ".git");
        if (Directory.Exists(gitDir))
        {
            _gitWatcher = TryCreateWatcher(gitDir);
            if (_gitWatcher != null)
            {
                _gitWatcher.Created += OnGitEvent;
                _gitWatcher.Changed += OnGitEvent;
                _gitWatcher.Deleted += OnGitEvent;
                _gitWatcher.Renamed += OnGitRenamed;
                _gitWatcher.Error += OnError;
            }
        }
    }

    private static FileSystemWatcher? TryCreateWatcher(string path)
    {
        try
        {
            return new FileSystemWatcher(path)
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.FileName
                             | NotifyFilters.DirectoryName
                             | NotifyFilters.LastWrite
                             | NotifyFilters.Size
                             | NotifyFilters.CreationTime,
                InternalBufferSize = FswBufferBytes,
                EnableRaisingEvents = true,
            };
        }
        catch
        {
            // Best-effort: a repo on a disconnected drive, an unreadable path, etc.
            // We just won't notice external changes for this repo. The user can still
            // refresh by switching repos or performing an in-app op.
            return null;
        }
    }

    private void OnTreeEvent(object sender, FileSystemEventArgs e)
    {
        if (IsUnderGit(e.FullPath)) return;
        ScheduleWorkingTree();
    }

    private void OnTreeRenamed(object sender, RenamedEventArgs e)
    {
        if (IsUnderGit(e.FullPath) && IsUnderGit(e.OldFullPath)) return;
        ScheduleWorkingTree();
    }

    private void OnGitEvent(object sender, FileSystemEventArgs e)
        => ClassifyGitChange(ToGitRelativePath(e.FullPath));

    private void OnGitRenamed(object sender, RenamedEventArgs e)
    {
        ClassifyGitChange(ToGitRelativePath(e.FullPath));
        ClassifyGitChange(ToGitRelativePath(e.OldFullPath));
    }

    private void ClassifyGitChange(string? gitRelativePath)
    {
        if (gitRelativePath == null) return;

        // NOTE: `.git/index` is deliberately not mapped. libgit2's read-side status call
        // (called from LocalChangesViewModel on every working-tree event) updates the
        // index stat cache, which would fire this watcher and cause an infinite loop.
        // The cost is that external `git add`/`git reset` from a terminal won't be
        // auto-detected; the user can refresh by switching repos or by making any
        // working-tree change.

        if (string.Equals(gitRelativePath, "HEAD", StringComparison.Ordinal)
            || string.Equals(gitRelativePath, "packed-refs", StringComparison.Ordinal)
            || string.Equals(gitRelativePath, "FETCH_HEAD", StringComparison.Ordinal)
            || string.Equals(gitRelativePath, "ORIG_HEAD", StringComparison.Ordinal)
            || string.Equals(gitRelativePath, "MERGE_HEAD", StringComparison.Ordinal)
            || gitRelativePath.StartsWith("refs/", StringComparison.Ordinal))
        {
            ScheduleRefs();
        }
        // .git/objects/**, .git/logs/**, .git/lfs/**, .git/hooks/**, .git/index — ignored.
    }

    private bool IsUnderGit(string fullPath)
        => fullPath.StartsWith(_gitDirPrefix, PathCmp);

    private string? ToGitRelativePath(string fullPath)
    {
        if (!fullPath.StartsWith(_gitDirPrefix, PathCmp)) return null;
        return fullPath[_gitDirPrefix.Length..].Replace('\\', '/');
    }

    private void ScheduleWorkingTree()
    {
        if (Volatile.Read(ref _disposed) != 0) return;
        _workingTreeDebounce.Change(DebounceMs, Timeout.Infinite);
    }

    private void ScheduleRefs()
    {
        if (Volatile.Read(ref _disposed) != 0) return;
        _refsDebounce.Change(DebounceMs, Timeout.Infinite);
    }

    private void OnWorkingTreeDebounce()
    {
        if (Volatile.Read(ref _disposed) != 0) return;
        var repoId = _repo.Id;
        _dispatcher.Post(() =>
        {
            if (Volatile.Read(ref _disposed) != 0) return;
            _bus.Broadcast(new WorkingTreeChangedMessage(repoId));
        });
    }

    private void OnRefsDebounce()
    {
        if (Volatile.Read(ref _disposed) != 0) return;
        var repoId = _repo.Id;
        _dispatcher.Post(() =>
        {
            if (Volatile.Read(ref _disposed) != 0) return;
            _bus.Broadcast(new RefsChangedMessage(repoId));
        });
    }

    private void OnError(object sender, ErrorEventArgs e)
    {
        // Internal buffer overflowed and events were dropped (huge churn — typically a
        // build or a checkout touching thousands of files). Schedule both channels so
        // the UI reconciles via a full reload rather than staying stale.
        ScheduleWorkingTree();
        ScheduleRefs();
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;

        if (_treeWatcher != null)
        {
            _treeWatcher.EnableRaisingEvents = false;
            _treeWatcher.Created -= OnTreeEvent;
            _treeWatcher.Changed -= OnTreeEvent;
            _treeWatcher.Deleted -= OnTreeEvent;
            _treeWatcher.Renamed -= OnTreeRenamed;
            _treeWatcher.Error -= OnError;
            _treeWatcher.Dispose();
        }
        if (_gitWatcher != null)
        {
            _gitWatcher.EnableRaisingEvents = false;
            _gitWatcher.Created -= OnGitEvent;
            _gitWatcher.Changed -= OnGitEvent;
            _gitWatcher.Deleted -= OnGitEvent;
            _gitWatcher.Renamed -= OnGitRenamed;
            _gitWatcher.Error -= OnError;
            _gitWatcher.Dispose();
        }
        _workingTreeDebounce.Dispose();
        _refsDebounce.Dispose();
    }
}
