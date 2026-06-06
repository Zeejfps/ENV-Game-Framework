namespace ZGF.Observable;

/// <summary>
/// Posts work onto the UI thread. Background workers call <see cref="Post"/>; the host
/// app drains the queue once per frame on the UI thread. Use this instead of
/// <see cref="System.Threading.Volatile"/>/UI-thread polling when handing values from a
/// worker into a <see cref="State{T}"/> or other single-threaded UI state.
/// </summary>
public interface IUiDispatcher
{
    void Post(Action action);
}
