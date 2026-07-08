namespace ZGF.Gui.Desktop.Input;

/// <summary>
/// A window that participates in pointer-ownership arbitration. Implemented by
/// <see cref="DesktopInputSystem"/>. The arbiter needs two things from a window: whether the
/// OS cursor currently sits inside its on-screen rect (native frame included, so a window
/// occludes the one below it even under its title bar), and whether it currently holds OS focus.
/// </summary>
public interface IPointerWindow
{
    bool IsCursorInsideWindow();
    bool IsWindowFocused();
}

/// <summary>
/// Single source of truth for "which window owns the pointer right now".
///
/// Previously every window's input system independently rect-tested the cursor and
/// decided to hover. Because a context-menu popup overlaps the main window, the same
/// screen point is inside both — so both hovered it ("hover goes to the menu AND the
/// item underneath"). Menu modality was then faked by stealing focus and consuming
/// move events in the main input system; any path that slipped past that hack
/// re-introduced the double-hover. None of those were structurally prevented.
///
/// Here ownership is a single-valued function of the registered windows and the
/// cursor: the topmost (last-registered) eligible window under the cursor owns it, and
/// only it may hover or receive pointer dispatch. Menus register as modal; while any
/// modal is registered, base (non-modal) windows are denied ownership entirely. So the
/// main window cannot hover beneath an open menu by construction, and "two windows
/// hovered at once" is not a representable state.
/// </summary>
public sealed class PointerOwnershipArbiter
{
    private readonly record struct Participant(IPointerWindow Window, bool IsModal);

    // Registration order is z-order: index 0 is the bottom (the main window), the last
    // entry is the topmost (most recently shown popup). A submenu registers after its
    // parent menu, so it sits above it.
    private readonly List<Participant> _participants = new();

    public void Register(IPointerWindow window, bool isModal)
    {
        _participants.RemoveAll(p => p.Window == window);
        _participants.Add(new Participant(window, isModal));
    }

    public void Unregister(IPointerWindow window)
    {
        _participants.RemoveAll(p => p.Window == window);
    }

    public bool IsRegistered(IPointerWindow window)
    {
        foreach (var p in _participants)
            if (p.Window == window) return true;
        return false;
    }

    /// <summary>
    /// True when a modal (menu) is open and <paramref name="window"/> is a base
    /// (non-modal) window behind it. Such a window must suppress all hover/move
    /// dispatch. Modal windows themselves are never blocked.
    /// </summary>
    public bool IsBlockedByModal(IPointerWindow window)
    {
        if (!AnyModalOpen()) return false;
        foreach (var p in _participants)
            if (p.Window == window) return !p.IsModal;
        return false;
    }

    /// <summary>
    /// True iff <paramref name="window"/> is the topmost window under the cursor among
    /// those eligible to own it. While a modal is open only modal windows are eligible,
    /// so the menu chain owns the pointer and the base window never does.
    /// </summary>
    public bool OwnsPointer(IPointerWindow window)
    {
        var anyModal = AnyModalOpen();
        for (var i = _participants.Count - 1; i >= 0; i--)
        {
            var p = _participants[i];
            if (anyModal && !p.IsModal) continue;
            if (!p.Window.IsCursorInsideWindow()) continue;
            return p.Window == window;
        }
        return false;
    }

    /// <summary>
    /// The topmost open modal (the active menu in a chain), or null when none is open. Used to route
    /// keyboard to an open menu: only the menu window receives OS key events on platforms where it
    /// takes focus (macOS), so on the others the active window forwards keys here instead.
    /// </summary>
    public IPointerWindow? TopmostModal()
    {
        for (var i = _participants.Count - 1; i >= 0; i--)
            if (_participants[i].IsModal) return _participants[i].Window;
        return null;
    }

    /// <summary>
    /// Raised when a base (non-modal) window receives a pointer press while a modal menu is open
    /// above it — i.e. a click outside the menu. The context-menu host subscribes to dismiss the
    /// open menu chain. This is the reliable cross-window close: a press a non-modal window receives
    /// is, by construction, not on the menu (menu presses land on the menu's own window), so it
    /// fires even where the OS popup capture doesn't — a background WS_EX_NOACTIVATE popup opened
    /// away from the cursor, or a secondary window whose presses the host never sees directly.
    /// </summary>
    public event Action? OutsidePressDismiss;

    /// <summary>
    /// Reports a pointer press on <paramref name="window"/>. When a modal is open and the pressed
    /// window is a base window behind it (not the menu chain itself), raises
    /// <see cref="OutsidePressDismiss"/>. A no-op when no modal is open or the press is on the menu,
    /// so menu and submenu clicks never self-dismiss.
    /// </summary>
    public void NotifyPress(IPointerWindow window)
    {
        if (!AnyModalOpen()) return;
        foreach (var p in _participants)
            if (p.Window == window)
            {
                if (!p.IsModal) OutsidePressDismiss?.Invoke();
                return;
            }
    }

    /// <summary>
    /// Reports that some window's OS focus changed. When a modal menu is open and focus has left
    /// every arbitrated window — the whole app lost focus (alt-tab, another application, a
    /// title-bar or taskbar click that steals focus without a client press) — raises
    /// <see cref="OutsidePressDismiss"/> so the menu chain closes. A no-op while any window still
    /// holds focus, including the menu popup itself (which is the key window on macOS), so opening
    /// a menu never immediately self-closes.
    /// </summary>
    public void NotifyFocusChanged()
    {
        if (!AnyModalOpen()) return;
        foreach (var p in _participants)
            if (p.Window.IsWindowFocused()) return;
        OutsidePressDismiss?.Invoke();
    }

    /// <summary>
    /// Reports a press on a host window's non-client area — title bar, borders, or caption buttons.
    /// GLFW surfaces only client-area mouse events and grabbing a title bar changes no focus (the
    /// host window already holds it), so neither <see cref="NotifyPress"/> nor
    /// <see cref="NotifyFocusChanged"/> fires for it. Any non-client press is by definition outside
    /// the menu popup, so raises <see cref="OutsidePressDismiss"/> whenever a modal menu is open.
    /// </summary>
    public void NotifyNonClientPress()
    {
        if (AnyModalOpen()) OutsidePressDismiss?.Invoke();
    }

    private bool AnyModalOpen()
    {
        foreach (var p in _participants)
            if (p.IsModal) return true;
        return false;
    }
}
