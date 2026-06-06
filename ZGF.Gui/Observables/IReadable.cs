namespace ZGF.Observable;

/// <summary>
/// The read side of an observable value. Both <see cref="State{T}"/> and
/// <see cref="Derived{T}"/> implement this; view bindings accept it so they don't care
/// which kind of source they're reading.
/// </summary>
public interface IReadable<T>
{
    T Value { get; }
    IDisposable Subscribe(Action<T> handler);
}
