using ZGF.Desktop;
using ZGF.Gui.Desktop.Input;

namespace ZGF.Gui.Desktop.Controllers;

/// <summary>Moves an undecorated window from a client-drawn background, leaving child controls alone.</summary>
public sealed class WindowDragController(IWindow window, InputSystem input) : KeyboardMouseController
{
    /// <summary>Optional wheel-only/background controllers that may pass a drag through.
    /// Other descendant controllers block dragging even when they do not consume the press.</summary>
    public Predicate<IKeyboardMouseController>? IsBackgroundController { get; init; }

    private bool _dragging;
    private double _grabX, _grabY;

    public override void OnMouseButtonStateChanged(ref MouseButtonEvent e)
    {
        if (e.Phase != EventPhase.Bubbling || e.Button != MouseButton.Left) return;
        if (e.State == InputState.Pressed)
        {
            if (input.HoveredComponent is { } hovered && input.GetView(hovered) != input.GetView(this)
                && !(IsBackgroundController?.Invoke(hovered) ?? false)) return;
            window.GetCursorPosition(out _grabX, out _grabY);
            _dragging = true;
            input.StealFocus(this);
            e.Consume();
        }
        else if (e.State == InputState.Released && _dragging)
        {
            _dragging = false;
            input.Blur(this);
            e.Consume();
        }
    }

    public override void OnMouseMoved(ref MouseMoveEvent e)
    {
        if (!_dragging || e.Phase != EventPhase.Bubbling) return;
        if (!e.Mouse.IsButtonPressed(MouseButton.Left))
        {
            _dragging = false;
            input.Blur(this);
            return;
        }
        // Keep the original grab offset: incremental canvas deltas feed the window's own
        // movement back into the next input sample, causing jitter and DPI-dependent drift.
        window.GetPosition(out var x, out var y);
        window.GetCursorPosition(out var cursorX, out var cursorY);
        window.SetPosition(x + (int)Math.Round(cursorX - _grabX), y + (int)Math.Round(cursorY - _grabY));
        e.Consume();
    }

    public override void OnFocusLost() => _dragging = false;
}
