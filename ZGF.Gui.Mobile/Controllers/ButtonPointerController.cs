using System;
using ZGF.Gui.Mobile.Input;

namespace ZGF.Gui.Mobile.Controllers;

/// <summary>
/// A tap controller: fires <see cref="Clicked"/> when a touch lifts while still inside the
/// guarded view, and exposes pressed-state transitions so the view can show feedback. Because
/// the gesture captures to its down-target, a release is delivered here even if the finger
/// drifted off the view — so the release point is bounds-checked against the view, giving the
/// familiar "slide off to cancel" behaviour.
/// </summary>
public sealed class ButtonPointerController : MobileInputController
{
    private readonly View _view;

    public ButtonPointerController(View view)
    {
        _view = view;
    }

    /// <summary>Invoked on a completed tap (down then up, both inside the view).</summary>
    public Action? Clicked { get; set; }

    /// <summary>Invoked when the button enters/leaves its pressed state (e.g. to tint it).</summary>
    public Action<bool>? PressedChanged { get; set; }

    private bool _pressed;

    public override void OnPointerTouched(ref PointerEvent e)
    {
        SetPressed(true);
        e.Consume();
    }

    public override void OnPointerMoved(ref PointerEvent e)
    {
        // Track whether the finger is currently over the button so the pressed feedback
        // follows it sliding on and off, matching native buttons.
        SetPressed(_view.Position.ContainsPoint(e.Position));
    }

    public override void OnPointerReleased(ref PointerEvent e)
    {
        var insideBounds = _view.Position.ContainsPoint(e.Position);
        SetPressed(false);
        if (insideBounds)
        {
            Clicked?.Invoke();
            e.Consume();
        }
    }

    public override void OnPointerExited(ref PointerEvent e)
    {
        SetPressed(false);
    }

    private void SetPressed(bool pressed)
    {
        if (_pressed == pressed)
            return;
        _pressed = pressed;
        PressedChanged?.Invoke(pressed);
    }
}
