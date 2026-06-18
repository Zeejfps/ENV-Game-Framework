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
    /// Sugar over <see cref="WithController{TController}(IWidget, System.Func{IInteractable})"/> for a
    /// stateful widget whose state is its interaction target: attaches a DI-built
    /// <typeparamref name="TController"/> with the widget's <see cref="IWidget{TState}.State"/>
    /// injected as the <see cref="IInteractable"/>. The parent calls it on the widget
    /// (<c>checkbox.WithController&lt;KbmController&gt;()</c>), choosing the controller — and thus the
    /// input modality — while the widget only supplies the state. The covariant <c>State</c> is read
    /// lazily, after the widget builds.
    /// </summary>
    public static IWidget WithController<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TController>(
        this IWidget<IInteractable> widget)
        where TController : class, IKeyboardMouseController =>
        widget.WithController<TController>(() => widget.State);

    /// <summary>
    /// Attaches a controller built by DI for an interaction target resolved at attach time. The
    /// <paramref name="target"/> thunk runs after the widget builds — needed when the target is a
    /// widget's <see cref="IWidget{TState}.State"/>, which doesn't exist until then.
    /// </summary>
    public static IWidget WithController<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TController>(
        this IWidget widget, Func<IInteractable> target)
        where TController : class, IKeyboardMouseController =>
        new DiControllerAttachment(widget, (ctx, v) =>
        {
            var input = ctx.Require<InputSystem>();
            var scope = new Context(ctx);
            scope.AddService(target());
            for (var t = v.GetType(); t != null && typeof(View).IsAssignableFrom(t); t = t.BaseType)
                scope.AddService(t, v);
            v.UseController(input, scope.Require<TController>);
        });

    /// <summary>
    /// Attaches a controller built by DI for an already-built interaction <paramref name="target"/> —
    /// the eager counterpart to the thunk overload, for a target that exists at attach time (a peer
    /// widget's state, a test double). Resolves <typeparamref name="TController"/> from a child
    /// <see cref="Context"/> seeded with the target and the built view.
    /// </summary>
    public static IWidget WithController<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TController>(
        this IWidget widget, IInteractable target)
        where TController : class, IKeyboardMouseController =>
        widget.WithController<TController>(() => target);

    /// <summary>
    /// Attaches a DI-built <typeparamref name="TController"/> to a stateful widget, exposing the
    /// widget's covariant <see cref="IWidget{TState}.State"/> to the controller's constructor under the
    /// <typeparamref name="TTarget"/> key — a richer interaction surface than the single
    /// <see cref="IInteractable"/> the no-arg overload seeds. A <c>Widget&lt;RepoRowState&gt;</c> can be
    /// wired as an <c>IRepoRow</c> or an <c>INavigableRow</c> by naming the view here, so two controllers
    /// reach the same state through different surfaces. The owning <see cref="Context"/> is seeded too,
    /// so a controller that needs it (e.g. to open a context menu) resolves the real one rather than a
    /// fabricated transient.
    /// </summary>
    public static IWidget WithController<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TController, TTarget>(
        this IWidget<TTarget> widget)
        where TController : class, IKeyboardMouseController
        where TTarget : class =>
        new DiControllerAttachment(widget, (ctx, v) =>
        {
            var input = ctx.Require<InputSystem>();
            var scope = new Context(ctx);
            scope.AddService<TTarget>(widget.State);
            scope.AddService(ctx);
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
