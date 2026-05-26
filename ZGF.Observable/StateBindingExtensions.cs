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
}
