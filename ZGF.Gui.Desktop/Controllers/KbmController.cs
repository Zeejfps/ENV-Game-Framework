using ZGF.Gui.Desktop.Input;
using ZGF.Gui.Widgets;
using ZGF.KeyboardModule;

namespace ZGF.Gui.Desktop.Controllers;

/// <summary>
/// Translates keyboard/mouse input into the interaction state of an <see cref="IInteractable"/>:
/// hover and press, whose rising edge the widget reads as activation. One controller serves every
/// pressable widget — a checkbox, a button — by binding to its <see cref="IInteractable"/> state
/// rather than to the widget itself, so the same controller drives anything that exposes that
/// surface. The mobile counterpart would be a touch controller writing the same surface.
/// <para>When a button joins a focus ring (the owner calls <c>input.StealFocus(controller)</c>), the
/// controller reuses the hover chrome to highlight while focused, activates on Enter/Space, and
/// forwards Tab / Shift+Tab to <see cref="OnTab"/>/<see cref="OnShiftTab"/>. Buttons that are never
/// focused leave all of that dormant.</para>
/// </summary>
public sealed class KbmController(IInteractable target) : KeyboardMouseController
{
    private bool _focused;

    /// <summary>Focus-ring traversal hooks; set by the owner when the button is a ring stop.</summary>
    public Action? OnTab { get; set; }
    public Action? OnShiftTab { get; set; }

    public override void OnMouseEnter(ref MouseEnterEvent e)
    {
        if (!target.Enabled.Value) return;
        target.Hovered.Value = true;
    }

    public override void OnMouseExit(ref MouseExitEvent e)
    {
        // Keep the highlight while focused — a tabbed-to button shouldn't dim when the pointer leaves.
        if (_focused) return;
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

    public override void OnFocusGained()
    {
        _focused = true;
        target.Hovered.Value = true;
    }

    public override void OnFocusLost()
    {
        _focused = false;
        target.Hovered.Value = false;
        target.Pressed.Value = false;
    }

    public override void OnKeyboardKeyStateChanged(ref KeyboardKeyEvent e)
    {
        if (!_focused) return;
        if (e.Phase != EventPhase.Bubbling) return;

        if (e.Key is KeyboardKey.Enter or KeyboardKey.NumpadEnter or KeyboardKey.Space)
        {
            if (!target.Enabled.Value) return;
            target.Pressed.Value = e.State == InputState.Pressed;
            e.Consume();
        }
        else if (e.Key == KeyboardKey.Tab && e.State == InputState.Pressed && (OnTab != null || OnShiftTab != null))
        {
            if ((e.Modifiers & InputModifiers.Shift) != 0) OnShiftTab?.Invoke();
            else OnTab?.Invoke();
            e.Consume();
        }
    }
}
