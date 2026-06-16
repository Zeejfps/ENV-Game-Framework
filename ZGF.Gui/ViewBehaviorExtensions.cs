namespace ZGF.Gui;

public static class ViewBehaviorExtensions
{
    /// <summary>
    /// Attaches a disposable whose lifetime tracks the view's mounted period: created on
    /// mount, disposed on unmount. For view-scoped helpers (tooltips, scroll-sync controllers)
    /// that are neither a ViewModel (<see cref="UseViewModel{TVm}(View, Func{TVm}, Action{TVm})"/>)
    /// nor an input controller (UseController). Dependencies are captured by the factory
    /// closure at construction time.
    /// </summary>
    public static void Use<T>(this View view, Func<T> factory)
        where T : IDisposable
    {
        view.Behaviors.Add(new ScopedBehavior<T>(factory));
    }

    public static void UseViewModel<TVm>(
        this View view,
        Func<TVm> factory,
        Action<TVm> bind)
        where TVm : IDisposable
    {
        view.Behaviors.Add(new ViewModelBehavior<TVm>(factory, bind));
    }

    /// <summary>
    /// Creates a <typeparamref name="TVm"/> via <paramref name="factory"/> and binds it
    /// to <paramref name="target"/>. The VM's lifetime tracks the host view (created on
    /// mount, disposed on unmount). Pass <c>this</c> as the target for the self-bound
    /// case; pass a child view for the parent-owned case.
    /// </summary>
    public static void UseViewModel<TVm>(
        this View host,
        Func<TVm> factory,
        IBind<TVm> target)
        where TVm : class, IDisposable
    {
        host.Behaviors.Add(new ViewModelBehavior<TVm>(factory, target.Bind));
    }
}
