namespace ZGF.Gui.Immediate;

/// <summary>Brings <c>await</c> back to the UI thread: what is awaited there continues through
/// <paramref name="post"/>, which runs it on the UI thread before the next frame.</summary>
public sealed class UiSynchronizationContext(Action<Action> post) : SynchronizationContext
{
    public override void Post(SendOrPostCallback callback, object? state)
    {
        post(() => callback(state));
    }

    public override void Send(SendOrPostCallback callback, object? state)
    {
        throw new NotSupportedException("Waiting for the UI thread could deadlock it; post instead.");
    }

    public override SynchronizationContext CreateCopy() => this;

    /// <summary>Makes this the current thread's context until the returned scope is disposed.</summary>
    public Scope Install()
    {
        var previous = Current;
        SetSynchronizationContext(this);
        return new Scope(previous);
    }

    public readonly struct Scope(SynchronizationContext? previous) : IDisposable
    {
        public void Dispose() => SetSynchronizationContext(previous);
    }
}
