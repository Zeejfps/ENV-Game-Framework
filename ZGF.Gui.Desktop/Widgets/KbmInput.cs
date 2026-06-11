using ZGF.Gui.Desktop.Controllers;
using ZGF.Gui.Desktop.Input;
using ZGF.Gui.Widgets;

namespace ZGF.Gui.Desktop.Widgets;

public delegate void MouseEnterEventHandler(ref MouseEnterEvent e);
public delegate void MouseExitEventHandler(ref MouseExitEvent e);
public delegate void MouseButtonEventHandler(ref MouseButtonEvent e);
public delegate void MouseMoveEventHandler(ref MouseMoveEvent e);
public delegate void MouseWheelEventHandler(ref MouseWheelScrolledEvent e);
public delegate void KeyboardKeyEventHandler(ref KeyboardKeyEvent e);

/// <summary>
/// Desktop input as a widget: wraps a child and attaches keyboard/mouse handling to the
/// child's built view for its mounted lifetime. The view tree itself stays input-agnostic —
/// this widget is the desktop-specific interpretation of it, the way a TouchInput widget
/// would be the mobile one.
///
/// Three tiers, combinable:
/// - Semantic callbacks (<see cref="OnClick"/>, <see cref="OnHoverEnter"/>/<see cref="OnHoverExit"/>)
///   fire once per gesture, during the bubble phase; click consumes the event.
/// - Raw handlers (<see cref="OnMouseButton"/>, <see cref="OnKey"/>, ...) see every phase and
///   control consumption themselves.
/// - <see cref="Controller"/> attaches a stateful controller built against the child's view,
///   created per mount and disposed per unmount.
/// </summary>
public sealed record KbmInput : Widget
{
    public required IWidget Child { get; init; }

    public Action? OnClick { get; init; }
    public Action? OnHoverEnter { get; init; }
    public Action? OnHoverExit { get; init; }

    public MouseEnterEventHandler? OnMouseEnter { get; init; }
    public MouseExitEventHandler? OnMouseExit { get; init; }
    public MouseButtonEventHandler? OnMouseButton { get; init; }
    public MouseMoveEventHandler? OnMouseMove { get; init; }
    public MouseWheelEventHandler? OnMouseWheel { get; init; }
    public KeyboardKeyEventHandler? OnKey { get; init; }
    public Action? OnFocusGained { get; init; }
    public Action? OnFocusLost { get; init; }

    public EventPhaseFilter Phases { get; init; } = EventPhaseFilter.Both;

    public Func<View, IKeyboardMouseController>? Controller { get; init; }

    protected override View CreateView(Context ctx)
    {
        var input = ctx.Require<InputSystem>();
        var view = Child.BuildView(ctx);

        if (HasHandlers)
            view.UseController(input, new DelegateKbmController(this), Phases);
        if (Controller != null)
            view.UseController(input, () => Controller(view), Phases);

        return view;
    }

    private bool HasHandlers =>
        OnClick != null || OnHoverEnter != null || OnHoverExit != null ||
        OnMouseEnter != null || OnMouseExit != null || OnMouseButton != null ||
        OnMouseMove != null || OnMouseWheel != null || OnKey != null ||
        OnFocusGained != null || OnFocusLost != null;
}

internal sealed class DelegateKbmController(KbmInput spec) : KeyboardMouseController
{
    public override void OnMouseEnter(ref MouseEnterEvent e)
    {
        if (e.Phase == EventPhase.Bubbling)
            spec.OnHoverEnter?.Invoke();
        spec.OnMouseEnter?.Invoke(ref e);
    }

    public override void OnMouseExit(ref MouseExitEvent e)
    {
        if (e.Phase == EventPhase.Bubbling)
            spec.OnHoverExit?.Invoke();
        spec.OnMouseExit?.Invoke(ref e);
    }

    public override void OnMouseButtonStateChanged(ref MouseButtonEvent e)
    {
        if (spec.OnClick != null
            && e.Phase == EventPhase.Bubbling
            && e.Button == MouseButton.Left
            && e.State == InputState.Pressed)
        {
            spec.OnClick();
            e.Consume();
        }
        if (!e.IsConsumed)
            spec.OnMouseButton?.Invoke(ref e);
    }

    public override void OnMouseMoved(ref MouseMoveEvent e)
    {
        spec.OnMouseMove?.Invoke(ref e);
    }

    public override void OnMouseWheelScrolled(ref MouseWheelScrolledEvent e)
    {
        spec.OnMouseWheel?.Invoke(ref e);
    }

    public override void OnKeyboardKeyStateChanged(ref KeyboardKeyEvent e)
    {
        spec.OnKey?.Invoke(ref e);
    }

    public override void OnFocusGained()
    {
        spec.OnFocusGained?.Invoke();
    }

    public override void OnFocusLost()
    {
        spec.OnFocusLost?.Invoke();
    }
}
