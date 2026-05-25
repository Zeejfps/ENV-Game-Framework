using ZGF.Gui;
using ZGF.Observable;

namespace GitGui;

/// <summary>
/// Base for view models built around an immutable state record. Centralizes the patterns
/// that otherwise pile up in every VM: per-field <see cref="Derived{T}"/> slices over a
/// single <see cref="State{T}"/>, a generation-guarded background-op runner that posts
/// results back through an <see cref="IUiDispatcher"/>, a subscription bag for upstream
/// observables/messages, and disposal of all of the above.
///
/// Subclasses construct the initial state via the base ctor, declare slices with
/// <see cref="Slice"/> in ctor order (slice notification fires in subscription order, so
/// order here matters when downstream rendering is order-sensitive), mutate state through
/// <see cref="Update"/>, and route async work through <see cref="RunBackground"/>.
/// </summary>
internal abstract class ViewModelBase<TState> : IDisposable
{
    private readonly List<IDisposable> _slices = new();

    protected IUiDispatcher Dispatcher { get; }
    protected SubscriptionGroup Subscriptions { get; } = new();
    protected GenerationGuard Gen { get; } = new();
    protected State<TState> State { get; }

    protected ViewModelBase(IUiDispatcher dispatcher, TState initial)
    {
        Dispatcher = dispatcher;
        State = new State<TState>(initial);
    }

    /// <summary>
    /// Declares a per-field projection over <see cref="State"/>. Tracked for disposal in
    /// reverse construction order. Slices notify in the order they're created — declare
    /// dependents before consumers when the downstream view relies on apply ordering.
    /// </summary>
    protected IReadable<T> Slice<T>(Func<TState, T> selector)
    {
        var derived = new Derived<T>(() => selector(State.Value));
        _slices.Add(derived);
        return derived;
    }

    protected void Update(Func<TState, TState> reducer)
        => State.Value = reducer(State.Value);

    /// <summary>
    /// Runs <paramref name="work"/> on a worker thread; on completion, posts
    /// <paramref name="onResult"/> to the UI thread. The continuation is dropped if the
    /// generation has advanced (repo switched, newer op started), so stale results never
    /// clobber fresher state. The work tuple lets callers report in-band errors without
    /// throwing; a thrown exception is captured as its <c>Message</c>.
    /// </summary>
    protected void RunBackground<T>(
        Func<(T? Result, string? Error)> work,
        Action<T?, string?> onResult)
    {
        var gen = Gen.Bump();
        var dispatcher = Dispatcher;
        Task.Run(() =>
        {
            T? result = default;
            string? errorMsg = null;
            try
            {
                (result, errorMsg) = work();
            }
            catch (Exception ex)
            {
                errorMsg = ex.Message;
            }

            dispatcher.Post(() =>
            {
                if (Gen.IsStale(gen)) return;
                onResult(result, errorMsg);
            });
        });
    }

    public virtual void Dispose()
    {
        // Bump first so any in-flight worker that resolves after Dispose sees a stale gen
        // and exits without touching state or firing notifications.
        Gen.Bump();
        Subscriptions.Dispose();
        for (var i = _slices.Count - 1; i >= 0; i--)
            _slices[i].Dispose();
        _slices.Clear();
    }
}
