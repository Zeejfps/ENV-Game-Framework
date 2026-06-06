namespace ZGF.Observable;

public static class StateBindingExtensions
{
    /// <summary>
    /// Forwards values from <paramref name="source"/> into <paramref name="target"/>. Fires
    /// immediately with the current value, then on every change. When the source is disposed
    /// (e.g. its owning view model goes away), the forwarding goes dormant — no leak.
    /// </summary>
    public static IDisposable BindTo<T>(this State<T> target, IReadable<T> source)
        => source.Subscribe(v => target.Value = v);

    /// <summary>
    /// Forwards values from <paramref name="source"/> through <paramref name="project"/> into
    /// <paramref name="target"/>. Fires immediately with the projected current value, then on
    /// every change.
    /// </summary>
    public static IDisposable BindTo<TSource, TTarget>(this State<TTarget> target,
        IReadable<TSource> source, Func<TSource, TTarget> project)
        => source.Subscribe(v => target.Value = project(v));

    /// <summary>
    /// Two-way sync between two states. Edits to either propagate to the other; the
    /// equality guard inside <see cref="State{T}.Value"/> prevents infinite recursion.
    /// On initial bind, <paramref name="source"/> wins (its value is pushed into
    /// <paramref name="target"/>). Call shape mirrors view ↔ VM binding: pass the VM
    /// state as <paramref name="source"/> so the view follows the model.
    /// </summary>
    public static IDisposable BindTwoWay<T>(this State<T> target, State<T> source)
    {
        var subSourceToTarget = source.Subscribe(v => target.Value = v);
        Action<T> handlerTargetToSource = v => source.Value = v;
        target.Changed += handlerTargetToSource;
        return new Subscription(() =>
        {
            subSourceToTarget.Dispose();
            target.Changed -= handlerTargetToSource;
        });
    }
}
