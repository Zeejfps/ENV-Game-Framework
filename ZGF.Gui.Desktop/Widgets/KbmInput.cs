using System.Numerics;
using ZGF.Gui.Desktop.Controllers;
using ZGF.Gui.Desktop.Input;
using ZGF.Gui.Widgets;

namespace ZGF.Gui.Desktop.Widgets;

/// <summary>
/// Desktop input as a widget: wraps a child and attaches keyboard/mouse handling to the
/// child's built view for its mounted lifetime. The view tree itself stays input-agnostic —
/// this widget is the desktop-specific interpretation of it, the way a TouchInput widget
/// would be the mobile one.
///
/// Four tiers, combinable:
/// - Semantic callbacks (<see cref="OnClick"/>, <see cref="OnHoverEnter"/>/<see cref="OnHoverExit"/>)
///   fire once per gesture, during the bubble phase; click consumes the event.
/// - Drag callbacks (<see cref="OnDragStart"/>/<see cref="OnDrag"/>/<see cref="OnDragEnd"/>)
///   run on a framework-owned <see cref="DragRecognizer"/> created per mount; the recognizer
///   holds the gesture state and the focus dance.
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

    public Action? OnDragStart { get; init; }
    public Action<Vector2>? OnDrag { get; init; }
    public Action? OnDragEnd { get; init; }
    /// <summary>Cursor travel (px) before a press becomes a drag. 0 = drag starts on press.</summary>
    public float DragThreshold { get; init; }

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

        var handlers = new KbmHandlers
        {
            OnClick = OnClick,
            OnHoverEnter = OnHoverEnter,
            OnHoverExit = OnHoverExit,
            OnMouseEnter = OnMouseEnter,
            OnMouseExit = OnMouseExit,
            OnMouseButton = OnMouseButton,
            OnMouseMove = OnMouseMove,
            OnMouseWheel = OnMouseWheel,
            OnKey = OnKey,
            OnFocusGained = OnFocusGained,
            OnFocusLost = OnFocusLost,
        };
        if (handlers.HasHandlers)
            view.UseController(input, handlers, Phases);

        if (OnDrag != null || OnDragStart != null || OnDragEnd != null)
        {
            view.UseController(input, () => new DragRecognizer(input)
            {
                Threshold = DragThreshold,
                DragStarted = OnDragStart,
                Dragged = OnDrag,
                DragEnded = OnDragEnd,
            }, Phases);
        }

        if (Controller != null)
            view.UseController(input, () => Controller(view), Phases);

        return view;
    }
}
