namespace ZGF.Gui.Desktop.Input;

/// <summary>
/// A window that participates in pointer-ownership arbitration. Implemented by
/// <see cref="DesktopInputSystem"/>. The only thing the arbiter needs from a window is
/// whether the OS cursor currently sits inside that window's client bounds.
/// </summary>
public interface IPointerWindow
{
    bool IsCursorInsideWindow();
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

    private bool AnyModalOpen()
    {
        foreach (var p in _participants)
            if (p.IsModal) return true;
        return false;
    }
}
