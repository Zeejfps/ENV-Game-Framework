namespace ZGF.Observable;

/// <summary>
/// A bindable command — an action plus the observable predicate that gates it. Implementations
/// (<see cref="Command"/>, async variants, etc.) are interchangeable from a binding consumer's
/// point of view: a button or keyboard controller can take any <see cref="ICommand"/> and wire
/// click/key dispatch through <see cref="Execute"/> while reflecting <see cref="CanExecute"/>
/// in its enabled state. <see cref="Execute"/> must be a no-op when <see cref="CanExecute"/>
/// is false, so consumers never have to check first.
/// </summary>
public interface ICommand
{
    IReadable<bool> CanExecute { get; }
    void Execute();
}
