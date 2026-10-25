using System.ComponentModel;

namespace ZGF.ILSpy;

/// <summary>How a wait on the themes folder ended.</summary>
public enum ThemeFolderEventKind
{
    /// <summary>Watching stopped, or was not running.</summary>
    Stopped,

    /// <summary>The watched files changed.</summary>
    Changed,

    /// <summary>The system stopped telling of changes, as when the folder was removed; nothing is watched since.</summary>
    Lost,
}

/// <summary>What <see cref="ThemeFolderWatch.NextChange"/> completes with; <see cref="Reason"/> says why
/// a watch was <see cref="ThemeFolderEventKind.Lost"/>.</summary>
public readonly record struct ThemeFolderEvent(ThemeFolderEventKind Kind, string Reason = "");

/// <summary>Watches a folder of the user's themes for changes to the files of the kinds it is made for,
/// such as the theme files of the themes folder. <see cref="NextChange"/> completes once those files
/// changed and then stayed quiet for a moment, so the several writes of one save count once, or once the
/// watch is lost. What happens while no one waits stays pending until the next <see cref="NextChange"/>
/// takes it. The file system tells of changes on other threads; this only completes a task there, for
/// the UI thread to await.</summary>
public sealed class ThemeFolderWatch
{
    private const string ThemeFilter = "*" + ThemeFile.Extension;
    private const int QuietMilliseconds = 200;

    private static readonly Task<ThemeFolderEvent> Changed = Task.FromResult(new ThemeFolderEvent(ThemeFolderEventKind.Changed));
    private static readonly Task<ThemeFolderEvent> Stopped = Task.FromResult(new ThemeFolderEvent(ThemeFolderEventKind.Stopped));

    private readonly Lock _lock = new();
    private FileSystemWatcher? _watcher;
    private readonly IReadOnlyList<string> _filters;
    private readonly bool _includesSubfolders;
    private Timer? _quiet;
    private TaskCompletionSource<ThemeFolderEvent>? _waiter;
    private bool _isChangePending;
    private string? _pendingLoss;

    /// <summary>Watches the color theme files at the top of a folder.</summary>
    public ThemeFolderWatch() : this([ThemeFilter], includesSubfolders: false)
    {
    }

    /// <summary>Watches the files <paramref name="filters"/> match, such as <c>*.json</c>, in a folder, and
    /// in the folders inside it when <paramref name="includesSubfolders"/>.</summary>
    public ThemeFolderWatch(IReadOnlyList<string> filters, bool includesSubfolders)
    {
        _filters = filters;
        _includesSubfolders = includesSubfolders;
    }

    /// <summary>Starts watching <paramref name="folder"/> in place of the folder watched before. Returns
    /// why the system cannot watch it, such as a folder gone or a limit on watches reached, and then
    /// watches nothing; else null.</summary>
    public string? Start(string folder)
    {
        Stop();
        try
        {
            Watch(folder);
            return null;
        }
        catch (Exception e) when (IsWatchFailure(e))
        {
            Stop();
            return DescribeFailure(folder, e);
        }
    }

    private void Watch(string folder)
    {
        var watcher = new FileSystemWatcher(folder);
        foreach (var filter in _filters)
            watcher.Filters.Add(filter);
        watcher.IncludeSubdirectories = _includesSubfolders;
        var quiet = new Timer(OnQuiet);
        watcher.Changed += OnChanged;
        watcher.Created += OnChanged;
        watcher.Deleted += OnChanged;
        watcher.Renamed += OnChanged;
        watcher.Error += OnError;
        lock (_lock)
        {
            _quiet = quiet;
            _watcher = watcher;
        }
        watcher.EnableRaisingEvents = true;
    }

    private static bool IsWatchFailure(Exception e) =>
        e is IOException or ArgumentException or UnauthorizedAccessException or PlatformNotSupportedException or Win32Exception;

    private static string DescribeFailure(string folder, Exception e) => $"{folder}{Environment.NewLine}{e.Message}";

    /// <summary>Completes once the watched files changed or the watch was lost, at once when either is
    /// pending, or as stopped when watching stops first or is not running.</summary>
    public Task<ThemeFolderEvent> NextChange()
    {
        lock (_lock)
        {
            if (_pendingLoss != null)
                return TakePendingLoss();
            if (_watcher == null)
                return Stopped;
            if (_isChangePending)
                return TakePendingChange();
            _waiter ??= new TaskCompletionSource<ThemeFolderEvent>(TaskCreationOptions.RunContinuationsAsynchronously);
            return _waiter.Task;
        }
    }

    private Task<ThemeFolderEvent> TakePendingLoss()
    {
        var loss = new ThemeFolderEvent(ThemeFolderEventKind.Lost, _pendingLoss!);
        _pendingLoss = null;
        return Task.FromResult(loss);
    }

    private Task<ThemeFolderEvent> TakePendingChange()
    {
        _isChangePending = false;
        return Changed;
    }

    /// <summary>Stops watching, ending the wait as stopped and dropping what is pending.</summary>
    public void Stop()
    {
        var waiter = Close(null);
        waiter?.TrySetResult(new ThemeFolderEvent(ThemeFolderEventKind.Stopped));
    }

    /// <summary>Lets go of the watcher and its timer and drops what is pending, keeping
    /// <paramref name="loss"/> pending when no one waits to be told; returns the waiter to tell. Does
    /// nothing when <paramref name="lost"/>, the watcher that failed, is no longer the one watching.</summary>
    private TaskCompletionSource<ThemeFolderEvent>? Close(string? loss, FileSystemWatcher? lost = null)
    {
        FileSystemWatcher? watcher;
        Timer? quiet;
        TaskCompletionSource<ThemeFolderEvent>? waiter;
        lock (_lock)
        {
            if (IsReplaced(lost))
                return null;
            watcher = _watcher;
            quiet = _quiet;
            waiter = _waiter;
            _watcher = null;
            _quiet = null;
            _waiter = null;
            _isChangePending = false;
            _pendingLoss = waiter == null ? loss : null;
        }
        watcher?.Dispose();
        quiet?.Dispose();
        return waiter;
    }

    private void OnChanged(object sender, FileSystemEventArgs e)
    {
        WaitForQuiet();
    }

    /// <summary>Reads the folder again after the system dropped changes it could not keep up with; on
    /// any other error the system has stopped telling, so the watch is lost.</summary>
    private void OnError(object sender, ErrorEventArgs e)
    {
        if (e.GetException() is InternalBufferOverflowException)
        {
            WaitForQuiet();
            return;
        }
        LoseWatch((FileSystemWatcher)sender, e.GetException());
    }

    private void LoseWatch(FileSystemWatcher watcher, Exception error)
    {
        var reason = DescribeFailure(watcher.Path, error);
        var waiter = Close(reason, watcher);
        waiter?.TrySetResult(new ThemeFolderEvent(ThemeFolderEventKind.Lost, reason));
    }

    private bool IsReplaced(FileSystemWatcher? lost)
    {
        if (lost == null)
            return false;
        return !ReferenceEquals(lost, _watcher);
    }

    private void WaitForQuiet()
    {
        lock (_lock)
            _quiet?.Change(QuietMilliseconds, Timeout.Infinite);
    }

    /// <summary>Hands the settled change to whoever waits for it, else keeps it pending.</summary>
    private void OnQuiet(object? state)
    {
        TaskCompletionSource<ThemeFolderEvent>? waiter;
        lock (_lock)
        {
            if (_watcher == null)
                return;
            waiter = _waiter;
            _waiter = null;
            if (waiter == null)
                _isChangePending = true;
        }
        waiter?.TrySetResult(new ThemeFolderEvent(ThemeFolderEventKind.Changed));
    }
}
