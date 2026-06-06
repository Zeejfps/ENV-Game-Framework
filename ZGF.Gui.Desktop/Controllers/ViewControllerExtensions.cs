using ZGF.Gui.Desktop.Input;

namespace ZGF.Gui.Desktop.Controllers;

public static class ViewControllerExtensions
{
    public static void UseController<T>(this View view, Func<Context, T> factory)
        where T : IKeyboardMouseController
    {
        view.Behaviors.Add(new ControllerBehavior<T>(factory));
    }

    public static void UseController<T>(this View view, Func<Context, T> factory, EventPhaseFilter phaseFilter)
        where T : IKeyboardMouseController
    {
        view.Behaviors.Add(new ControllerBehavior<T>(factory, phaseFilter));
    }
}
