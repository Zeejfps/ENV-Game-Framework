using ZGF.Gui.Desktop.Input;
using ZGF.Gui.Widgets;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Desktop.Controllers;

/// <summary>
/// Translates keyboard/mouse input into the interaction state of an <see cref="IInteractable"/>:
/// hover and press, whose rising edge the widget reads as activation. One controller serves every
/// pressable widget — a checkbox, a button — so widgets declare <see cref="IInteractable"/> and
/// attach this instead of hand-rolling input. The mobile counterpart would be a touch controller
/// writing the same surface.
/// </summary>
public sealed class KbmController(IInteractable target) : KeyboardMouseController
{
    private bool _focused;

    public override void OnMouseEnter(ref MouseEnterEvent e)
    {
        if (!target.Enabled.Value) return;
        target.Hovered.Value = true;
    }

    public override void OnMouseExit(ref MouseExitEvent e)
    {
        target.Hovered.Value = false;
        target.Pressed.Value = false;
    }

    public override void OnMouseButtonStateChanged(ref MouseButtonEvent e)
    {
        if (!target.Enabled.Value) return;
        if (e.Phase != EventPhase.Bubbling) return;
        if (e.Button != MouseButton.Left) return;

        if (e.State == InputState.Pressed)
        {
            target.Pressed.Value = true;
            e.Consume();
        }
        else if (e.State == InputState.Released)
        {
            target.Pressed.Value = false;
        }
    }

    public override void OnFocusGained() => _focused = true;

    public override void OnFocusLost()
    {
        _focused = false;
        target.Pressed.Value = false;
    }

    public override void OnKeyboardKeyStateChanged(ref KeyboardKeyEvent e)
    {
        if (!_focused) return;
        if (!target.Enabled.Value) return;
        if (e.Phase != EventPhase.Bubbling) return;
        if (e.Key is not (KeyboardKey.Enter or KeyboardKey.NumpadEnter or KeyboardKey.Space)) return;

        target.Pressed.Value = e.State == InputState.Pressed;
        e.Consume();
    }
}
