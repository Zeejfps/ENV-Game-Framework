namespace ZGF.Observable;

/// <summary>
/// Bundles an action with the observable predicate that gates it. Views bind to a command
/// (typically via <c>BindCommand</c>) so click dispatch and enabled-state derive from one
/// source instead of being wired separately. <see cref="Execute"/> is a no-op when
/// <see cref="CanExecute"/> is false — the gate is enforced both at the view layer (button
/// disabled) and here, so callers never need to check first.
/// </summary>
public sealed class Command
{
    private readonly Action _execute;
    public IReadable<bool> CanExecute { get; }

    public Command(Action execute, IReadable<bool>? canExecute = null)
    {
        _execute = execute;
        CanExecute = canExecute ?? new State<bool>(true);
    }

    public void Execute()
    {
        if (CanExecute.Value) _execute();
    }
}
