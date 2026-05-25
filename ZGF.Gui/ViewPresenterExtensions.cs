namespace ZGF.Gui;

public static class ViewPresenterExtensions
{
    public static void UsePresenter<T>(this View view, Func<Context, T> factory)
        where T : IDisposable
    {
        view.Behaviors.Add(new PresenterBehavior<T>(factory));
    }

    public static void UseViewModel<TVm>(
        this View view,
        Func<Context, TVm> factory,
        Action<TVm, SubscriptionGroup> bind)
        where TVm : IDisposable
    {
        view.Behaviors.Add(new ViewModelBehavior<TVm>(factory, bind));
    }
}
