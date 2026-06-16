using ZGF.Gui.Desktop.Input;
using ZGF.Gui.Widgets;

namespace ZGF.Gui.Desktop.Controllers;

/// <summary>
/// Widget combinator mirroring <see cref="ViewControllerExtensions.UseController{T}(View, InputSystem, System.Func{T})"/>:
/// wires an input controller onto the child's built view. Lives in the desktop layer because
/// the input system does.
/// </summary>
public static class WidgetControllerExtensions
{
    public static IWidget WithController<T>(this IWidget widget, InputSystem input, Func<T> factory)
        where T : IKeyboardMouseController =>
        new ControllerAttachment(widget, v => v.UseController(input, factory));

    private sealed record ControllerAttachment(IWidget Child, Action<View> Attach) : IWidget
    {
        public View BuildView(Context ctx)
        {
            var v = Child.BuildView(ctx);
            Attach(v);
            return v;
        }
    }
}
