using ZGF.Geometry;

namespace ZGF.Gui.Desktop.Input;

/// <summary>
/// A window, as the <see cref="ImeCoordinator"/> drives it: whether it holds OS focus, whether
/// anything inside it holds keyboard focus, the native IME switches, and the canvas-to-screen
/// mapping the caret rect goes through.
/// <para>
/// Deliberately not <see cref="IImeHost"/>. That is the request a text field makes — "I am editing,
/// here is my caret" — and a field cannot know which window will end up composing for it. This is
/// the interface the answer is carried out against.
/// </para>
/// </summary>
public interface IImeWindow : IPointerWindow
{
    /// <summary>True when a component in this window's tree holds keyboard focus. A searchable
    /// context menu holds it while never holding OS focus, which is the case that makes this and
    /// <see cref="IPointerWindow.IsWindowFocused"/> worth telling apart.</summary>
    bool HasKeyboardFocus { get; }

    /// <summary>Whether this window is editing text, and so whether the OS IME may consume its
    /// keystrokes to compose.</summary>
    void SetTextInputFocus(bool focused);

    /// <summary>Anchors the OS candidate window on the caret, in screen coordinates. The caret may
    /// belong to a field in a different window than the one composing, so it arrives in the only
    /// space the two share.</summary>
    void SetImeCursorRect(RectI screenRect);

    /// <summary>Discards any in-flight composition rather than committing it.</summary>
    void ResetImeComposition();

    /// <summary>Maps this window's canvas coordinates to screen coordinates.</summary>
    RectI CanvasToScreen(RectF canvasRect);
}

/// <summary>
/// Decides which window the OS IME composes against and where its candidate window sits.
///
/// <para>A field cannot decide this for itself, because the window it lives in is not always the
/// window that composes. A searchable context menu lives in its own popup window, but a borderless
/// popup never takes OS keyboard focus on Windows (WS_EX_NOACTIVATE): the keys land on the host
/// window, so the IME must be enabled <em>there</em>, and the composition it produces is forwarded
/// back to the menu. Enable the IME on the popup instead and nothing composes at all — the search
/// box takes Latin and not CJK.</para>
///
/// <para>So the IME mode is not a toggle a field owns. Fields report only that they are editing
/// (<see cref="SetFieldEditing"/>), and the mode is re-derived from focus every tick. That is what
/// makes it self-healing: a menu closing above a still-editing commit box does not have to remember
/// to hand the IME back, because the next tick observes that the commit box is now the one
/// editing.</para>
/// </summary>
public sealed class ImeCoordinator
{
    private readonly PointerOwnershipArbiter _arbiter;
    private readonly List<IImeWindow> _windows = new();

    // Every field currently editing, keyed by the window it lives in, with its caret in that
    // window's canvas coordinates. More than one can edit at once — a menu's search box opens over
    // a commit box that keeps its caret — and that is exactly what stops the menu's close from
    // switching the IME off underneath the commit box.
    private readonly Dictionary<IImeWindow, RectF> _editingCarets = new();

    private IImeWindow? _composing;
    private RectI? _pushedCaret;

    public ImeCoordinator(PointerOwnershipArbiter arbiter) => _arbiter = arbiter;

    public void Register(IImeWindow window)
    {
        if (!_windows.Contains(window))
            _windows.Add(window);
    }

    public void Unregister(IImeWindow window)
    {
        _windows.Remove(window);
        _editingCarets.Remove(window);
        if (ReferenceEquals(_composing, window))
        {
            _composing = null;
            _pushedCaret = null;
        }
    }

    /// <summary>
    /// The popup that owns typing while it is open, or null when typing belongs to the OS-focused
    /// window itself. A searchable context menu takes keyboard focus but not OS focus, so keys,
    /// characters and compositions all arrive at the host window and have to be handed on; a plain
    /// menu focuses nothing and leaves the host window's typing alone. The window backends route
    /// all three event kinds through this, so they cannot disagree about who is typing.
    /// </summary>
    public IImeWindow? FocusedModal() =>
        _arbiter.TopmostModal() is IImeWindow modal && modal.HasKeyboardFocus ? modal : null;

    /// <summary>Reports that a field in <paramref name="window"/> has begun or ended an edit
    /// session. The IME is off outside a field, so that a CJK layout doesn't start composing on the
    /// keys that navigate a list.</summary>
    public void SetFieldEditing(IImeWindow window, bool editing)
    {
        if (editing)
            _editingCarets.TryAdd(window, default);
        else
            _editingCarets.Remove(window);
    }

    /// <summary>Updates the editing field's caret, in its own window's canvas coordinates. Ignored
    /// for a window with no field editing — the caret is only meaningful as part of a session.</summary>
    public void SetFieldCaret(IImeWindow window, RectF canvasCaret)
    {
        if (_editingCarets.ContainsKey(window))
            _editingCarets[window] = canvasCaret;
    }

    /// <summary>Abandons any in-flight composition. It lives on whichever window is composing, which
    /// is not necessarily the window whose field asked.</summary>
    public void ResetComposition() => _composing?.ResetImeComposition();

    /// <summary>
    /// Re-asserts the IME state from focus. Called once per tick, after the windows have updated, so
    /// it sees the frame's opened and closed popups rather than a state a field remembered to set.
    /// </summary>
    public void Update()
    {
        var focused = FocusedWindow();
        var target = FocusedModal() ?? focused;

        // The composition happens on the OS-focused window (that is where the keys go), driven by
        // the caret of whichever field is being typed into — the two are the same window except
        // while a popup menu holds the keyboard.
        var composing = focused != null && target != null && _editingCarets.ContainsKey(target)
            ? focused
            : null;

        if (!ReferenceEquals(composing, _composing))
        {
            // Native calls, so only on a change. Turning the old one off first keeps at most one
            // window composing — a second one left enabled would compose on keys it never sees.
            _composing?.SetTextInputFocus(false);
            composing?.SetTextInputFocus(true);
            _composing = composing;
            _pushedCaret = null;
        }

        if (composing == null || target == null)
            return;

        // The caret is in the canvas of the window whose field is editing, so it converts through
        // that window; the composing window then rebases it into its own client area. Screen
        // coordinates are the only space the two share when they are different windows.
        var caret = target.CanvasToScreen(_editingCarets[target]);
        if (caret == _pushedCaret)
            return;
        _pushedCaret = caret;
        composing.SetImeCursorRect(caret);
    }

    /// <summary>The app's OS-focused window, or null when the app is in the background.</summary>
    private IImeWindow? FocusedWindow()
    {
        foreach (var window in _windows)
            if (window.IsWindowFocused())
                return window;
        return null;
    }
}
