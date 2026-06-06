namespace ZGF.Observable;

/// <summary>
/// A source of values that can notify when its value becomes stale.
/// Used by <see cref="Derived{T}"/> to track which sources to subscribe to.
/// </summary>
public interface IInvalidatable
{
    event Action Invalidated;
}
