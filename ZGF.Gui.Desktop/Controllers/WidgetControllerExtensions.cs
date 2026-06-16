using System.Diagnostics.CodeAnalysis;
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

    /// <summary>
    /// Resolves <typeparamref name="T"/> from <paramref name="ctx"/> at mount time — for
    /// view-agnostic controllers whose constructor dependencies are all container services
    /// (e.g. global keybind handlers). A fresh transient is built on each attach and disposed
    /// on detach, exactly as the factory overload does. Use the factory overload instead when
    /// the controller needs a locally-built view.
    /// </summary>
    public static IWidget WithController<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
        this IWidget widget, Context ctx)
        where T : class, IKeyboardMouseController =>
        widget.WithController(ctx.Require<InputSystem>(), () => ctx.Require<T>());

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
