using ZGF.Geometry;

namespace ZGF.Gui.Desktop.Input;

/// <summary>
/// The window's IME, as the focused text field needs it. Implemented by the desktop input system,
/// which owns the window and the canvas-to-window coordinate mapping; a text field reaches it
/// through <see cref="InputSystem.ImeHost"/> rather than knowing about windows itself.
/// </summary>
public interface IImeHost
{
    /// <summary>
    /// Whether the OS IME may compose. A field turns this on while it is being edited and off when
    /// it is not: left on, a CJK IME would start composing on the keys that drive list navigation.
    /// </summary>
    void SetImeEnabled(bool enabled);

    /// <summary>
    /// Where the caret is, in canvas coordinates, so the OS candidate window can sit against it
    /// rather than at the window origin. Worth calling whenever the caret moves while composing.
    /// </summary>
    void SetImeCaretRect(RectF caretRect);

    /// <summary>
    /// Abandons any in-flight composition. The text is discarded rather than committed, so this is
    /// for losing the field (blur, Escape), not for accepting what the user typed.
    /// </summary>
    void ResetComposition();
}
