namespace ZGF.ILSpy;

/// <summary>What the screen asks of the app state, which <see cref="UiState"/> queues while it draws
/// and <see cref="IlSpyApp"/> runs, in the order queued, once the frame is drawn. Made only through
/// <see cref="UiState.Later(Action{AppState})"/> and its overload, from a <c>static</c> lambda handed
/// what it acts on, so that a view drawn every frame allocates nothing until it asks.</summary>
public abstract class AppCommand
{
    private AppCommand()
    {
    }

    public abstract void Run(AppState appState);

    internal static AppCommand Of(Action<AppState> run) => new Plain(run);

    internal static AppCommand Of<T>(T subject, Action<AppState, T> run) => new On<T>(subject, run);

    private sealed class Plain(Action<AppState> run) : AppCommand
    {
        public override void Run(AppState appState) => run(appState);
    }

    private sealed class On<T>(T subject, Action<AppState, T> run) : AppCommand
    {
        public override void Run(AppState appState) => run(appState, subject);
    }
}
