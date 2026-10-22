using System.Runtime.CompilerServices;

namespace ZGF.ILSpy;

/// <summary>What work run in the background gave: its value, or the error it failed with.</summary>
public readonly record struct Outcome<T>(T? Value, Exception? Error);

/// <summary>Runs work on the thread pool for the UI thread to await.</summary>
public static class Background
{
    public static BackgroundWork<T> Run<T>(Func<T> work) => new(Task.Run(work));
}

/// <summary>Work running on the thread pool. Awaited on the UI thread, it always continues on a
/// later turn of the UI thread, even when the work is already done, with its <see cref="Outcome{T}"/>.</summary>
public readonly struct BackgroundWork<T>(Task<T> task)
{
    public Awaiter GetAwaiter() => new(task);

    public readonly struct Awaiter(Task<T> task) : INotifyCompletion
    {
        public bool IsCompleted => false;

        public void OnCompleted(Action continuation)
        {
            var ui = SynchronizationContext.Current;
            if (ui == null)
                throw new InvalidOperationException("Background work is awaited on the UI thread.");
            task.ContinueWith(_ => ui.Post(Continue, continuation), TaskScheduler.Default);
        }

        private static void Continue(object? continuation)
        {
            var action = (Action)continuation!;
            action();
        }

        public Outcome<T> GetResult()
        {
            if (task.IsCompletedSuccessfully)
                return new Outcome<T>(task.Result, null);
            return new Outcome<T>(default, FindError());
        }

        private Exception FindError()
        {
            if (task.Exception == null)
                return new OperationCanceledException();
            return task.Exception.GetBaseException();
        }
    }
}
