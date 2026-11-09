using System.Collections.Concurrent;
using System.Runtime.ExceptionServices;

namespace ZGF.Gui.Testing.Gpu;

/// <summary>One dedicated thread that runs work handed to it, one item at a time, while the caller waits.</summary>
internal sealed class RenderThread
{
    private static readonly TimeSpan IdleInterval = TimeSpan.FromMilliseconds(100);

    private readonly BlockingCollection<Job> _queue = new();
    private readonly Lock _gate = new();
    private readonly Thread _thread;
    private readonly Action<Action> _scope;
    private Action? _idle;
    private bool _closed;

    /// <param name="scope">Runs each queued item, given the item; it must call it exactly once.</param>
    public RenderThread(string name, Action<Action>? scope = null)
    {
        _scope = scope ?? (work => work());
        _thread = new Thread(Loop) { Name = name, IsBackground = true };
        _thread.Start();
    }

    public bool IsCurrent => Environment.CurrentManagedThreadId == _thread.ManagedThreadId;

    /// <summary>Runs <paramref name="idle"/> on this thread whenever it has waited <see cref="IdleInterval"/>
    /// without work, and after each item.</summary>
    public void OnIdle(Action idle) => Run(() => _idle = idle);

    public void Run(Action work) => Run<object?>(() =>
    {
        work();
        return null;
    });

    /// <summary>Runs <paramref name="work"/> on this thread and returns its result, or rethrows what it threw.
    /// Runs inline when already on this thread.</summary>
    /// <exception cref="ObjectDisposedException">The thread no longer accepts work.</exception>
    public T Run<T>(Func<T> work)
    {
        if (IsCurrent)
            return work();

        T result = default!;
        var job = new Job(() => result = work());
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_closed, this);
            _queue.Add(job);
        }
        job.Done.Wait();
        job.Failure?.Throw();
        return result;
    }

    /// <summary>Runs <paramref name="work"/> as <see cref="Run(Action)"/> does, or returns false without
    /// running it once the thread no longer accepts work.</summary>
    public bool TryRun(Action work)
    {
        if (IsCurrent)
        {
            work();
            return true;
        }

        var job = new Job(work);
        lock (_gate)
        {
            if (_closed) return false;
            _queue.Add(job);
        }
        job.Done.Wait();
        job.Failure?.Throw();
        return true;
    }

    /// <summary>Stops accepting work, runs everything already accepted, then <paramref name="terminal"/> as
    /// the last item, and waits for the thread to end, even through an interrupt. Rethrows what
    /// <paramref name="terminal"/> threw, or else the interrupt.</summary>
    public void Shutdown(Action terminal)
    {
        if (IsCurrent)
            throw new InvalidOperationException("The render thread cannot shut itself down.");

        var job = new Job(terminal);
        lock (_gate)
        {
            if (_closed)
                throw new InvalidOperationException("The render thread is already shut down.");
            _closed = true;
            _queue.Add(job);
            _queue.CompleteAdding();
        }

        var interrupted = JoinThroughInterrupts();
        _queue.Dispose();
        job.Failure?.Throw();
        if (interrupted)
            throw new ThreadInterruptedException("Interrupted while the render thread shut down; it has finished.");
    }

    private bool JoinThroughInterrupts()
    {
        var interrupted = false;
        while (true)
        {
            try
            {
                _thread.Join();
                return interrupted;
            }
            catch (ThreadInterruptedException)
            {
                // The render thread is still running GL; the wait is retried and the interrupt rethrown after.
                interrupted = true;
            }
        }
    }

    private void Loop()
    {
        while (!_queue.IsCompleted)
        {
            if (_queue.TryTake(out var job, IdleInterval))
                job.Execute(_scope);
            _idle?.Invoke();
        }
    }

    private sealed class Job(Action work)
    {
        public ManualResetEventSlim Done { get; } = new();
        public ExceptionDispatchInfo? Failure { get; private set; }

        public void Execute(Action<Action> scope)
        {
            try
            {
                scope(work);
            }
            catch (Exception e)
            {
                // Handed back to the waiting caller, which rethrows it with its original stack.
                Failure = ExceptionDispatchInfo.Capture(e);
            }
            finally
            {
                Done.Set();
            }
        }
    }
}
