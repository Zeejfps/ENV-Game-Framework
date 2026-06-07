namespace ZGF.Observable;

public static class ReadableExtensions
{
    /// <summary>
    /// Projects a reactive value into a derived slice. The returned observable re-fires only
    /// when the projected value actually changes — <see cref="Derived{T}"/> auto-tracks the
    /// <paramref name="source"/> read and dedups by equality. The result is itself an
    /// <see cref="IDisposable"/>; cache it and dispose it (it holds a subscription to
    /// <paramref name="source"/>) rather than calling Select per access.
    /// </summary>
    public static IReadable<TOut> Select<TIn, TOut>(this IReadable<TIn> source, Func<TIn, TOut> selector)
        => new Derived<TOut>(() => selector(source.Value));
}
