namespace ZGF.Gui.Immediate;

/// <summary>What <see cref="ImmediateInspection"/> asks of a host running as usual, each on its UI thread.</summary>
internal interface IInspectedHost
{
    /// <summary>Runs <paramref name="work"/> on the UI thread between frames. Safe from any thread.</summary>
    void Post(Action work);

    /// <summary>Whether the calling thread is the UI thread.</summary>
    bool OnUiThread { get; }

    /// <summary>Asks for a frame. Safe from any thread.</summary>
    void Invalidate();

    Ui Ui { get; }

    /// <summary>Writes the main window's next frame to <paramref name="path"/> as a PNG, then runs <paramref name="done"/>.</summary>
    void RequestScreenshot(string path, Action done);
}

/// <summary>A frame an inspection captured: its snapshot, and the main window's pixels from that same frame, as a PNG.</summary>
public sealed record InspectedFrame(SemanticSnapshot Snapshot, byte[] Png);

/// <summary>
/// Reads an app running as usual, from any thread but its UI thread: the snapshot of a frame and the main window's
/// pixels. Neither injects input, moves focus or shows anything; each asks for one frame, which a hidden or minimized
/// window may never draw, so each waits at most a timeout. Calls fail once the app has closed.
/// </summary>
public sealed class ImmediateInspection
{
    private readonly IInspectedHost _host;
    private readonly object _gate = new();
    private readonly List<TaskCompletionSource<SemanticSnapshot>> _snapshots = new();
    private readonly List<CaptureRequest> _captures = new();
    private readonly SemaphoreSlim _captureTurn = new(1, 1);
    private bool _closed;
    private long _frame;

    internal ImmediateInspection(IInspectedHost host)
    {
        _host = host;
    }

    /// <summary>Whether the calling thread is the app's UI thread, where every call throws.</summary>
    public bool OnUiThread => _host.OnUiThread;

    /// <summary>The number of the last frame drawn, as <see cref="SemanticSnapshot.Frame"/> numbers it.</summary>
    public long FrameNumber
    {
        get
        {
            lock (_gate)
                return _frame;
        }
    }

    /// <summary>The next frame as a test sees it.</summary>
    /// <exception cref="TimeoutException">No frame was drawn within <paramref name="timeout"/>.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancel"/> fired first.</exception>
    /// <exception cref="ObjectDisposedException">The app has closed.</exception>
    public SemanticSnapshot Snapshot(TimeSpan timeout, CancellationToken cancel = default)
    {
        RequireOffUiThread();
        var waiting = new TaskCompletionSource<SemanticSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (_gate)
        {
            if (_closed)
                throw Closed();
            _snapshots.Add(waiting);
        }
        _host.Post(() =>
        {
            _host.Ui.BuildsSemantics = true;
            _host.Invalidate();
        });
        try
        {
            return Await(waiting.Task, timeout, cancel, "a frame to snapshot");
        }
        catch (Exception e) when (e is TimeoutException or OperationCanceledException)
        {
            lock (_gate)
                _snapshots.Remove(waiting);
            throw;
        }
    }

    /// <summary>The next frame: its snapshot, and the main window's pixels as the screen shows them, from that one
    /// frame. Popups and windows of their own are not in the pixels.</summary>
    /// <exception cref="TimeoutException">No frame was drawn within <paramref name="timeout"/>.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancel"/> fired first.</exception>
    /// <exception cref="InvalidOperationException">The render backend could not read the frame back.</exception>
    /// <exception cref="ObjectDisposedException">The app has closed.</exception>
    public InspectedFrame Capture(TimeSpan timeout, CancellationToken cancel = default)
    {
        RequireOffUiThread();
        var deadline = DateTime.UtcNow + timeout;
        if (!_captureTurn.Wait(timeout, cancel))
            throw NoFrame(timeout, "another capture to finish");
        try
        {
            var request = new CaptureRequest(Path.Combine(Path.GetTempPath(), $"zgf-inspect-{Guid.NewGuid():N}.png"));
            lock (_gate)
            {
                if (_closed)
                    throw Closed();
                _captures.Add(request);
            }
            _host.Post(() => Arm(request));
            try
            {
                var left = deadline - DateTime.UtcNow;
                return Await(request.Done.Task, left > TimeSpan.Zero ? left : TimeSpan.Zero, cancel, "a frame to capture");
            }
            catch (Exception e) when (e is TimeoutException or OperationCanceledException)
            {
                Retire(request);
                throw;
            }
        }
        finally
        {
            _captureTurn.Release();
        }
    }

    /// <summary>A frame ended: hands its snapshot to whoever waits for one, and to each capture whose frame it is, and
    /// stops building snapshots once nobody waits. UI thread only.</summary>
    internal void FrameEnded()
    {
        var snapshot = _host.Ui.Snapshot;
        TaskCompletionSource<SemanticSnapshot>[] waiting = [];
        bool wanted;
        lock (_gate)
        {
            _frame = _host.Ui.FrameNumber;
            if (snapshot != null)
            {
                waiting = _snapshots.ToArray();
                _snapshots.Clear();
                foreach (var capture in _captures)
                {
                    if (capture.Armed && capture.Snapshot == null)
                        capture.Snapshot = snapshot;
                }
            }
            wanted = _snapshots.Count > 0 || _captures.Count > 0;
        }
        if (snapshot != null)
        {
            foreach (var waiter in waiting)
                waiter.TrySetResult(snapshot);
        }
        if (!wanted)
            _host.Ui.BuildsSemantics = false;
    }

    /// <summary>The app is closing: fails every wait and capture, and every call after. Safe from any thread.</summary>
    internal void Shutdown()
    {
        TaskCompletionSource<SemanticSnapshot>[] waiting;
        CaptureRequest[] captures;
        lock (_gate)
        {
            _closed = true;
            waiting = _snapshots.ToArray();
            _snapshots.Clear();
            captures = _captures.ToArray();
            _captures.Clear();
            foreach (var capture in captures)
                capture.Retired = true;
        }
        foreach (var waiter in waiting)
            waiter.TrySetException(Closed());
        foreach (var capture in captures)
            capture.Done.TrySetException(Closed());
    }

    /// <summary>Asks for the frame <paramref name="request"/> captures, unless it was given up on meanwhile. UI thread only.</summary>
    private void Arm(CaptureRequest request)
    {
        lock (_gate)
        {
            if (request.Retired)
                return;
            request.Armed = true;
        }
        _host.Ui.BuildsSemantics = true;
        _host.RequestScreenshot(request.Path, () => Written(request));
    }

    /// <summary>The backend has written, or failed to write, the frame <paramref name="request"/> asked for. The file is
    /// this method's alone: nothing else deletes it, since before the request is armed there is none and after it only
    /// the backend knows when writing ends, which is now. It reads and deletes the file whatever became of the request,
    /// and never throws out of the render callback it runs in; a file it cannot read fails the request instead.</summary>
    private void Written(CaptureRequest request)
    {
        byte[]? png = null;
        Exception? unread = null;
        try
        {
            png = File.ReadAllBytes(request.Path);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            unread = e;
        }
        try
        {
            File.Delete(request.Path);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            unread ??= e;
        }
        SemanticSnapshot? snapshot;
        lock (_gate)
        {
            if (request.Retired)
                return;
            _captures.Remove(request);
            snapshot = request.Snapshot;
        }
        if (png == null)
            request.Done.TrySetException(new InvalidOperationException("The render backend could not read the main window's frame back.", unread));
        else if (snapshot == null)
            request.Done.TrySetException(new InvalidOperationException("The captured frame built no snapshot."));
        else
            request.Done.TrySetResult(new InspectedFrame(snapshot, png));
    }

    /// <summary>Gives up on <paramref name="request"/>: nothing completes it after. Its file, if the backend writes one
    /// still, is deleted by <see cref="Written"/>.</summary>
    private void Retire(CaptureRequest request)
    {
        lock (_gate)
        {
            request.Retired = true;
            _captures.Remove(request);
        }
    }


    private void RequireOffUiThread()
    {
        if (_host.OnUiThread)
            throw new InvalidOperationException("ImmediateInspection was called on the app's UI thread, which would wait for itself; call it from another thread.");
    }

    private static T Await<T>(Task<T> task, TimeSpan timeout, CancellationToken cancel, string what)
    {
        try
        {
            return task.WaitAsync(timeout, cancel).GetAwaiter().GetResult();
        }
        catch (TimeoutException)
        {
            throw NoFrame(timeout, what);
        }
    }

    private static TimeoutException NoFrame(TimeSpan timeout, string what) =>
        new($"No frame came within {timeout.TotalSeconds:0.###}s for {what}: a hidden or minimized window draws none.");

    private static ObjectDisposedException Closed() => new(nameof(ImmediateInspection), "The app has closed.");

    /// <summary>One capture: armed on the UI thread once its frame is asked for, given that frame's snapshot as it ends,
    /// and retired when its caller gives up or the app closes, after which nothing completes it.</summary>
    private sealed class CaptureRequest(string path)
    {
        public string Path { get; } = path;
        public TaskCompletionSource<InspectedFrame> Done { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool Armed { get; set; }
        public bool Retired { get; set; }
        public SemanticSnapshot? Snapshot { get; set; }
    }
}
