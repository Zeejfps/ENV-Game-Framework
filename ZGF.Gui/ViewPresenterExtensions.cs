using System.Diagnostics.CodeAnalysis;

namespace ZGF.Gui;

public static class ViewPresenterExtensions
{
    public static void UsePresenter<T>(this View view, Func<Context, T> factory)
        where T : IDisposable
    {
        view.Behaviors.Add(new PresenterBehavior<T>(factory));
    }

    public static void UseBehavior<T>(this View view, Func<Context, T> factory)
        where T : IDisposable
    {
        view.Behaviors.Add(new PresenterBehavior<T>(factory));
    }

    public static void UseViewModel<TVm>(
        this View view,
        Func<Context, TVm> factory,
        Action<TVm> bind)
        where TVm : IDisposable
    {
        view.Behaviors.Add(new ViewModelBehavior<TVm>(factory, bind));
    }

    public static void UseViewModel<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TVm>(
        this View view,
        Action<TVm> bind)
        where TVm : class, IDisposable
    {
        view.Behaviors.Add(new ViewModelBehavior<TVm>(ctx => ctx.Create<TVm>(), bind));
    }

    /// <summary>
    /// Creates a <typeparamref name="TVm"/> via the host view's context and binds it
    /// to <paramref name="target"/>. The VM's lifetime tracks the host view (created on
    /// attach, disposed on detach). Pass <c>this</c> as the target for the self-bound
    /// case; pass a child view for the parent-owned case.
    /// </summary>
    public static void UseViewModel<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TVm>(
        this View host,
        IBind<TVm> target)
        where TVm : class, IDisposable
    {
        host.Behaviors.Add(new ViewModelBehavior<TVm>(ctx => ctx.Create<TVm>(), target.Bind));
    }
}
