namespace ZGF.Gui;

public static class ViewPresenterExtensions
{
    public static void UsePresenter<T>(this View view, Func<Context, T> factory)
        where T : IDisposable
    {
        view.Behaviors.Add(new PresenterBehavior<T>(factory));
    }
}
