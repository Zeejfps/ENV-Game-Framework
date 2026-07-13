using ZGF.Gui.Desktop.Input;

namespace ZGF.Gui.Desktop.Controllers;

public delegate void MouseEnterEventHandler(ref MouseEnterEvent e);
public delegate void MouseExitEventHandler(ref MouseExitEvent e);
public delegate void MouseButtonEventHandler(ref MouseButtonEvent e);
public delegate void MouseMoveEventHandler(ref MouseMoveEvent e);
public delegate void MouseWheelEventHandler(ref MouseWheelScrolledEvent e);
public delegate void KeyboardKeyEventHandler(ref KeyboardKeyEvent e);
public delegate void TextInputEventHandler(ref TextInputEvent e);

/// <summary>
/// Delegate-based controller: the handler tiers of <see cref="ZGF.Gui.Desktop.Widgets.KbmInput"/>
/// as a plain controller, for view-land code that wires input without a build context.
/// Semantic callbacks (<see cref="OnClick"/>, hover) fire once per gesture on the bubble
/// phase and click consumes; raw handlers see every phase and manage consumption themselves.
/// </summary>
public sealed class KbmHandlers : IKeyboardMouseController
{
    public Action? OnClick { get; init; }
    public Action? OnHoverEnter { get; init; }
    public Action? OnHoverExit { get; init; }

    public MouseEnterEventHandler? OnMouseEnter { get; init; }
    public MouseExitEventHandler? OnMouseExit { get; init; }
    public MouseButtonEventHandler? OnMouseButton { get; init; }
    public MouseMoveEventHandler? OnMouseMove { get; init; }
    public MouseWheelEventHandler? OnMouseWheel { get; init; }
    public KeyboardKeyEventHandler? OnKey { get; init; }
    public TextInputEventHandler? OnTextInput { get; init; }
    public Action? OnFocusGained { get; init; }
    public Action? OnFocusLost { get; init; }

    public bool HasHandlers =>
        OnClick != null || OnHoverEnter != null || OnHoverExit != null ||
        OnMouseEnter != null || OnMouseExit != null || OnMouseButton != null ||
        OnMouseMove != null || OnMouseWheel != null || OnKey != null ||
        OnTextInput != null || OnFocusGained != null || OnFocusLost != null;

    void IKeyboardMouseController.OnMouseEnter(ref MouseEnterEvent e)
    {
        if (e.Phase == EventPhase.Bubbling)
            OnHoverEnter?.Invoke();
        OnMouseEnter?.Invoke(ref e);
    }

    void IKeyboardMouseController.OnMouseExit(ref MouseExitEvent e)
    {
        if (e.Phase == EventPhase.Bubbling)
            OnHoverExit?.Invoke();
        OnMouseExit?.Invoke(ref e);
    }

    void IKeyboardMouseController.OnMouseButtonStateChanged(ref MouseButtonEvent e)
    {
        if (OnClick != null
            && e.Phase == EventPhase.Bubbling
            && e.Button == MouseButton.Left
            && e.State == InputState.Pressed)
        {
            OnClick();
            e.Consume();
        }
        if (!e.IsConsumed)
            OnMouseButton?.Invoke(ref e);
    }

    void IKeyboardMouseController.OnMouseMoved(ref MouseMoveEvent e)
    {
        OnMouseMove?.Invoke(ref e);
    }

    void IKeyboardMouseController.OnMouseWheelScrolled(ref MouseWheelScrolledEvent e)
    {
        OnMouseWheel?.Invoke(ref e);
    }

    void IKeyboardMouseController.OnKeyboardKeyStateChanged(ref KeyboardKeyEvent e)
    {
        OnKey?.Invoke(ref e);
    }

    void IKeyboardMouseController.OnTextInput(ref TextInputEvent e)
    {
        OnTextInput?.Invoke(ref e);
    }

    void IKeyboardMouseController.OnFocusGained()
    {
        OnFocusGained?.Invoke();
    }

    void IKeyboardMouseController.OnFocusLost()
    {
        OnFocusLost?.Invoke();
    }
}
