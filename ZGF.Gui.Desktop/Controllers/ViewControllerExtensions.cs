using ZGF.Gui.Desktop.Input;

namespace ZGF.Gui.Desktop.Controllers;

public static class ViewControllerExtensions
{
    /// <summary>
    /// Registers an input controller for the view's mounted lifetime: created on mount,
    /// registered with <paramref name="input"/> (the input system of the window the view is
    /// built for), unregistered and disposed on unmount. The input system comes from the
    /// window's build <see cref="Context"/> — a view is pinned to the window it was built for.
    /// </summary>
    public static void UseController<T>(this View view, InputSystem input, Func<T> factory)
        where T : IKeyboardMouseController
    {
        view.Behaviors.Add(new ControllerBehavior<T>(input, factory));
    }

    public static void UseController<T>(this View view, InputSystem input, Func<T> factory, EventPhaseFilter phaseFilter)
        where T : IKeyboardMouseController
    {
        view.Behaviors.Add(new ControllerBehavior<T>(input, factory, phaseFilter));
    }

    public static void UseController<T>(this View view, InputSystem input, T controller)
        where T : IKeyboardMouseController
    {
        view.Behaviors.Add(new ControllerBehavior<T>(input, controller));
    }

    public static void UseController<T>(this View view, InputSystem input, T controller, EventPhaseFilter phaseFilter)
        where T : IKeyboardMouseController
    {
        view.Behaviors.Add(new ControllerBehavior<T>(input, controller, phaseFilter));
    }
}
