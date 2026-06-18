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
    /// View-aware factory overload: <paramref name="factory"/> receives the child's built view, for
    /// controllers whose constructor needs the local view (drag targets, peer registration). Mirrors
    /// <see cref="ZGF.Gui.Widgets.WidgetExtensions.Use{T}(IWidget, System.Func{View, T})"/>.
    /// </summary>
    public static IWidget WithController<T>(this IWidget widget, InputSystem input, Func<View, T> factory)
        where T : IKeyboardMouseController =>
        new ControllerAttachment(widget, v => v.UseController(input, () => factory(v)));

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

    /// <summary>
    /// Sugar over <see cref="WithController{TController}(IWidget, IInteractableWidget)"/> for a widget
    /// that is its own target: attaches a DI-built <typeparamref name="TController"/> with the widget
    /// injected as the <see cref="IInteractableWidget"/>. Called on the widget itself by whoever creates
    /// it (<c>checkbox.WithController&lt;KbmController&gt;()</c>), collapsing widget and target.
    /// </summary>
    public static IWidget WithController<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TController>(
        this IInteractableWidget widget)
        where TController : class, IKeyboardMouseController =>
        widget.WithController<TController>(widget);

    /// <summary>
    /// Attaches a controller built by DI for an interaction <paramref name="target"/> — typically the
    /// widget itself (<c>tree.WithController&lt;KbmController&gt;(this)</c>). Resolves
    /// <typeparamref name="TController"/> from a child <see cref="Context"/> seeded with
    /// <paramref name="target"/> and the built view, so the controller's constructor receives the
    /// <see cref="IInteractableWidget"/> (and the view, if it asks for it) while any other dependencies
    /// come from the context. Created on mount, disposed on unmount, like the factory overloads.
    /// </summary>
    public static IWidget WithController<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TController>(
        this IWidget widget, IInteractableWidget target)
        where TController : class, IKeyboardMouseController =>
        new DiControllerAttachment(widget, (ctx, v) =>
        {
            var input = ctx.Require<InputSystem>();
            var scope = new Context(ctx);
            scope.AddService(target);
            for (var t = v.GetType(); t != null && typeof(View).IsAssignableFrom(t); t = t.BaseType)
                scope.AddService(t, v);
            v.UseController(input, scope.Require<TController>);
        });

    private sealed record ControllerAttachment(IWidget Child, Action<View> Attach) : IWidget
    {
        public View BuildView(Context ctx)
        {
            var v = Child.BuildView(ctx);
            Attach(v);
            return v;
        }
    }

    private sealed record DiControllerAttachment(IWidget Child, Action<Context, View> Attach) : IWidget
    {
        public View BuildView(Context ctx)
        {
            var v = Child.BuildView(ctx);
            Attach(ctx, v);
            return v;
        }
    }
}
