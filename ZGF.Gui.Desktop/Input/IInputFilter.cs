namespace ZGF.Gui.Desktop.Input;

/// <summary>
/// Sees a window's keys and mouse buttons before any controller does, wherever focus is. For
/// gestures that span several events and have to be recognised in one place whatever has focus: a
/// modifier tapped on its own, or a chord whose second stroke must not reach the focused editor.
/// A filter may consume a key, and then nothing else is sent it; a mouse button it only watches.
/// </summary>
public interface IInputFilter
{
    void OnKey(ref KeyboardKeyEvent e);

    void OnMouseButton(in MouseButtonEvent e);

    /// <summary>The window lost focus: whatever was held down will be released somewhere unseen.</summary>
    void OnWindowFocusLost();
}
