namespace ZGF.Gui.Immediate;

/// <summary>Work running off the UI thread that the UI is not ready without, such as a font loading:
/// the one test harness given it waits for each piece to finish, and for a frame after. Safe from any thread.</summary>
public sealed class BackgroundWork
{
    private readonly object _gate = new();
    private readonly List<string> _outstanding = new();
    private readonly List<(string Name, Exception Error)> _failures = new();
    private long _finished;
    private Action? _onFinished;

    /// <summary>Counts <paramref name="work"/>, which <paramref name="name"/> names in reports, as
    /// outstanding until it finishes.</summary>
    public void Track(string name, Task work)
    {
        lock (_gate)
            _outstanding.Add(name);
        work.ContinueWith(done => Finish(name, done), TaskScheduler.Default);
    }

    /// <summary>The names of the work still running.</summary>
    public IReadOnlyList<string> Outstanding
    {
        get
        {
            lock (_gate)
                return _outstanding.ToArray();
        }
    }

    /// <summary>Has <paramref name="onFinished"/> called, from any thread, each time a piece of work finishes.
    /// Throws when something is already attached.</summary>
    internal void Attach(Action onFinished)
    {
        lock (_gate)
        {
            if (_onFinished != null)
                throw new InvalidOperationException("This background work is already attached to another harness.");
            _onFinished = onFinished;
        }
    }

    /// <summary>How many pieces of work are running, and how many have finished so far.</summary>
    internal (int Outstanding, long Finished) Progress()
    {
        lock (_gate)
            return (_outstanding.Count, _finished);
    }

    /// <summary>The work that failed, with why, since the last call.</summary>
    internal (string Name, Exception Error)[] TakeFailures()
    {
        lock (_gate)
        {
            var failures = _failures.ToArray();
            _failures.Clear();
            return failures;
        }
    }

    private void Finish(string name, Task done)
    {
        Action? onFinished;
        lock (_gate)
        {
            _outstanding.Remove(name);
            if (done.Exception != null)
                _failures.Add((name, done.Exception));
            _finished++;
            onFinished = _onFinished;
        }
        onFinished?.Invoke();
    }
}
