using ZGF.Gui.Mobile.Input;

namespace ZGF.Gui.Mobile.Controllers;

/// <summary>
/// Fluent attachment of a touch controller to a view, the mobile parallel to
/// ViewControllerExtensions.UseController.
/// </summary>
public static class ViewPointerControllerExtensions
{
    public static void UsePointerController<T>(this View view, Func<Context, T> factory)
        where T : IPointerController
    {
        view.Behaviors.Add(new MobilePointerControllerBehavior<T>(factory));
    }

    public static void UsePointerController<T>(this View view, Func<Context, T> factory, PointerPhaseFilter phaseFilter)
        where T : IPointerController
    {
        view.Behaviors.Add(new MobilePointerControllerBehavior<T>(factory, phaseFilter));
    }
}
