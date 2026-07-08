using ZGF.Desktop;
using ZGF.Geometry;
using ZGF.Gui.Desktop.Input;
using ZGF.KeyboardModule;
using InputState = ZGF.Gui.Desktop.Input.InputState;
using MouseButton = ZGF.Gui.Desktop.Input.MouseButton;

namespace ZGF.Gui.Desktop;

public sealed class DesktopInputSystem : IPointerWindow
{
    private readonly IWindow _window;
    private readonly RenderedCanvasBase _canvas;
    private readonly PointerOwnershipArbiter? _arbiter;
    private readonly IWindowedApp? _app;
    private bool _pendingExitClear;
    // Buttons whose press was swallowed as a modal-dismiss click. The matching release is
    // part of the same gesture and must be swallowed too — dispatched alone, it would land
    // on whatever sits under the cursor now that the menu is gone.
    private readonly HashSet<MouseButton> _modalDismissButtons = new();

    public InputSystem InputSystem { get; } = new();
    public Mouse Mouse { get; } = new();
    public Action? OnAnyInput { get; set; }

    public DesktopInputSystem(IWindow window, RenderedCanvasBase canvas, PointerOwnershipArbiter? arbiter = null, IWindowedApp? app = null)
    {
        _window = window;
        _canvas = canvas;
        _arbiter = arbiter;
        _app = app;

        _window.OnKey += HandleKeyEvent;
        _window.OnMouseButton += HandleMouseButtonEvent;
        _window.OnScroll += HandleScrollEvent;
        _window.OnPointerEnter += HandleCursorEnter;
    }

    /// <summary>
    /// True when the OS cursor is within this window's full on-screen rect, native frame
    /// included. This is the arbiter's occlusion test, so it must cover everything this
    /// window visually obscures: with a client-only rect, a cursor parked on a window's
    /// title bar would let the window *behind* it claim pointer ownership and hover.
    /// Cursor coords are client-relative, so the frame extends the rect into negatives.
    /// </summary>
    public bool IsCursorInsideWindow()
    {
        _window.GetCursorPosition(out var x, out var y);
        _window.GetFrameSize(out var left, out var top, out var right, out var bottom);
        return x >= -left && y >= -top && x <= _window.Width + right && y <= _window.Height + bottom;
    }

    /// <summary>True when this window currently holds OS keyboard focus.</summary>
    public bool IsWindowFocused() => _window.IsFocused;

    /// <summary>
    /// Clear all transient input state so this instance can be reused for a fresh
    /// popup. Resets the underlying <see cref="InputSystem"/> (focus/hover/path) and
    /// the locally latched fields (pending exit-clear, last-known cursor point). Call
    /// when the owning popup window is returned to the pool — otherwise a leftover
    /// focused component keeps <see cref="Input.InputSystem.HasFocus"/> true and
    /// <see cref="Update"/> short-circuits before hover/click dispatch, leaving every
    /// subsequently pooled popup dead until the app restarts.
    /// </summary>
    public void Reset()
    {
        InputSystem.Reset();
        _pendingExitClear = false;
        _modalDismissButtons.Clear();
        Mouse.Point = new PointF(float.MinValue, float.MinValue);
    }

    private void HandleCursorEnter(bool entering)
    {
        // The OS only fires this on enter/exit transitions. When the cursor leaves
        // the window we can't safely send a synthetic MouseMove right now (we may
        // be inside the event-pump and the view tree isn't reentrant); defer the
        // hover-clear to the next Update() tick.
        if (!entering) _pendingExitClear = true;
    }

    public void Update()
    {
        // Hover here is polled (GetCursorPosition + bounds test), not event-driven, and
        // GLFW reports cursor coords relative to the window even when the window is
        // unfocused or covered by another application. Without this gate, hover keeps
        // dispatching — and spawning tooltips — while the app sits in the background,
        // merely because the cursor crosses the window's screen rect.
        if (!AnyAppWindowFocused())
        {
            if (!InputSystem.IsPointerCaptured)
            {
                InputSystem.ClearHover();
                Mouse.Point = new PointF(float.MinValue, float.MinValue);
            }
            return;
        }

        var managed = _arbiter != null && _arbiter.IsRegistered(this);

        // A modal (context menu) is open and this window sits behind it: suppress all
        // hover/move dispatch so views beneath the menu can't re-hover. This is the
        // structural replacement for the old focus-steal + consume-move hack — main
        // window hover is now off whenever a menu is open, by construction.
        if (managed && _arbiter!.IsBlockedByModal(this))
        {
            InputSystem.ClearHover();
            Mouse.Point = new PointF(float.MinValue, float.MinValue);
            return;
        }

        if (InputSystem.HasFocus)
        {
            _window.GetCursorPosition(out var capturedX, out var capturedY);
            var capturedPoint = WindowToGuiCoords(capturedX, capturedY);
            if (capturedPoint != Mouse.Point)
            {
                Mouse.Point = capturedPoint;
                var capturedEvent = new MouseMoveEvent
                {
                    Mouse = Mouse,
                    Phase = EventPhase.Capturing,
                };
                InputSystem.SendMouseMovedEvent(ref capturedEvent);
                OnAnyInput?.Invoke();
            }
            else if (!InputSystem.IsPointerCaptured)
            {
                // Cursor stationary and no drag owns the pointer: the tree may still have changed
                // under it since last frame (e.g. a click closed a tab and the next tab slid under
                // the cursor), so re-hit-test — this keeps hover, and the click-dispatch path built
                // from it, tracking what's now beneath the cursor without a mouse wiggle. Skipped
                // while a drag captures the pointer (scrollbar thumb, splitter): there, content
                // scrolling under a still cursor must NOT re-hover what slid beneath it. Keyboard
                // focus alone (a list, a text field) does not capture, so hover stays live for it.
                InputSystem.RefreshHover(Mouse);
            }
            _window.SetCursor(InputSystem.DesiredCursor);
            return;
        }

        // Hover belongs to exactly one window: the topmost under the cursor. If that
        // isn't us, drop any hover we hold and do nothing — no rect test, no dispatch.
        if (managed && !_arbiter!.OwnsPointer(this))
        {
            InputSystem.ClearHover();
            Mouse.Point = new PointF(float.MinValue, float.MinValue);
            return;
        }

        if (_pendingExitClear)
        {
            _pendingExitClear = false;
            var prev = Mouse.Point;
            Mouse.Point = new PointF(float.MinValue, float.MinValue);
            var exitEvent = new MouseMoveEvent
            {
                Mouse = Mouse,
                Phase = EventPhase.Capturing,
            };
            InputSystem.SendMouseMovedEvent(ref exitEvent);
            // Don't restore Mouse.Point — the cursor is genuinely outside; the
            // next Update with MouseHover=true will refresh from the window.
            _ = prev;
        }

        // Freeze hover state when the cursor leaves this window's bounds. Without
        // this, GetCursorPosition reports stale coords outside the window's rect and
        // the hover path would clear — defeating, e.g., tooltip hover while the
        // cursor sits over the tooltip's own popup window.
        //
        // Test the cursor against the window rect directly rather than via the OS
        // pointer-over attribute. That attribute updates only from OS cursor enter/leave
        // events, which fire solely on cursor *movement*: a context menu that pops up
        // directly under a stationary cursor never receives an enter event, so the menu
        // opens dead until the mouse is jiggled. A direct bounds check reports hover on
        // the first tick while still freezing once the cursor genuinely leaves the rect.
        _window.GetCursorPosition(out var mouseX, out var mouseY);
        var winWidth = _window.Width;
        var winHeight = _window.Height;
        var cursorInsideBounds =
            mouseX >= 0 && mouseY >= 0 && mouseX <= winWidth && mouseY <= winHeight;
        
        if (!cursorInsideBounds)
        {
            return;
        }
        
        var guiPoint = WindowToGuiCoords(mouseX, mouseY);
        var prevPoint = Mouse.Point;
        Mouse.Point = guiPoint;
        if (prevPoint == guiPoint)
        {
            InputSystem.RefreshHover(Mouse);
        }
        else
        {
            var e = new MouseMoveEvent
            {
                Mouse = Mouse,
                Phase = EventPhase.Capturing,
            };
            InputSystem.SendMouseMovedEvent(ref e);
            OnAnyInput?.Invoke();
        }

        _window.SetCursor(InputSystem.DesiredCursor);
    }

    private void HandleScrollEvent(double x, double y)
    {
        var e = new MouseWheelScrolledEvent
        {
            Mouse = Mouse,
            DeltaX = (float)x,
            DeltaY = (float)y,
            Phase = EventPhase.Capturing
        };
        InputSystem.SendMouseScrollEvent(ref e);
        OnAnyInput?.Invoke();
    }

    private void HandleMouseButtonEvent(int buttonIndex, InputAction action, KeyModifiers modifiers)
    {
        _window.GetCursorPosition(out var mouseX, out var mouseY);
        Mouse.Point = WindowToGuiCoords(mouseX, mouseY);
        var b = buttonIndex switch
        {
            0 => MouseButton.Left,
            1 => MouseButton.Right,
            2 => MouseButton.Middle,
            _ => new MouseButton(buttonIndex),
        };
        var s = MapState(action);

        if (s == InputState.Pressed)
        {
            Mouse.Press(b);
        }
        else
        {
            Mouse.Release(b);
        }

        var e = new MouseButtonEvent
        {
            Mouse = Mouse,
            Button = b,
            State = s,
            Modifiers = (InputModifiers)modifiers,
            Phase = EventPhase.Capturing,
        };
        // A press on a window that isn't the open menu is, by definition, outside it: ask the
        // arbiter to dismiss the menu chain. This is the reliable cross-window close — it covers
        // secondary windows (whose presses the menu host never sees) and the case where the OS
        // popup capture misses the click (a background WS_EX_NOACTIVATE menu opened away from the
        // cursor). The arbiter no-ops unless a modal is open above this (a base) window.
        if (s == InputState.Pressed)
            _arbiter?.NotifyPress(this);

        // While a modal menu is open and this window sits behind it, the press's only role is
        // to dismiss the menu (handled above). Our own hover path is frozen at whatever was
        // hovered when the menu opened — Update() clears hover but keeps _focusQueue — so
        // dispatching here would re-fire that stale path: reopening the just-closed menu
        // (left-click) or popping a context menu from a control the cursor isn't even over
        // (right-click). Skip local dispatch; the dismiss click is consumed. This check runs
        // after NotifyPress but still sees the modal as open — CloseAllImmediately only flags
        // the popup for release; the arbiter unregistration is deferred to the next Update().
        var blocked = _arbiter != null && _arbiter.IsBlockedByModal(this);
        if (s == InputState.Pressed)
        {
            if (blocked) _modalDismissButtons.Add(b);
            else _modalDismissButtons.Remove(b);
        }
        else if (_modalDismissButtons.Remove(b))
        {
            blocked = true;
        }

        if (!blocked)
            InputSystem.SendMouseButtonEvent(ref e);

        OnAnyInput?.Invoke();
    }

    private void HandleKeyEvent(KeyboardKey key, InputAction action, KeyModifiers mods)
    {
        var e = new KeyboardKeyEvent
        {
            Key = key,
            State = MapState(action),
            Modifiers = (InputModifiers)mods,
            Phase = EventPhase.Capturing
        };

        // An open context-menu popup owns the keyboard while it has a focused target (a search box):
        // popups can't take OS keyboard focus on every platform (Windows borderless menus are
        // WS_EX_NOACTIVATE), so OS keys land on whichever real window is active. Forward them to the
        // menu's own input system and skip local dispatch, so the menu's search/type-ahead works and
        // the host window's shortcuts stay dormant while it's up. Gated on the menu actually holding
        // focus, so a plain (non-searchable) menu doesn't swallow the host window's keys. When the
        // menu window itself is the one receiving keys (it took focus), TopmostModal() == this, so it
        // falls through and dispatches locally as usual.
        var modal = _arbiter?.TopmostModal();
        if (modal is DesktopInputSystem menu && !ReferenceEquals(menu, this) && menu.InputSystem.HasFocus)
        {
            menu.InputSystem.SendKeyboardKeyEvent(ref e);
            OnAnyInput?.Invoke();
            return;
        }

        InputSystem.SendKeyboardKeyEvent(ref e);
        OnAnyInput?.Invoke();
    }

    // True when any of the app's windows (main, secondary, or popup) holds OS focus —
    // i.e. the app is the active application. A click-through tooltip never takes key
    // focus, so during normal tooltip hover the main window still reports focused and
    // hover stays live. Instances constructed without an app (tests) are always active.
    private bool AnyAppWindowFocused()
    {
        if (_app == null) return true;
        var windows = _app.Windows;
        for (var i = 0; i < windows.Count; i++)
            if (windows[i].IsFocused) return true;
        return false;
    }

    private static InputState MapState(InputAction action) => action switch
    {
        InputAction.Press => InputState.Pressed,
        InputAction.Repeat => InputState.Pressed,
        InputAction.Release => InputState.Released,
        _ => throw new ArgumentOutOfRangeException(nameof(action), action, null),
    };

    private PointF WindowToGuiCoords(double windowX, double windowY)
    {
        var width = _window.Width;
        var height = _window.Height;
        var scaleX = _canvas.Width / (float)width;
        var scaleY = _canvas.Height / (float)height;
        var screenX = windowX * scaleX;
        var screenY = (height - windowY) * scaleY;
        return new PointF((float)screenX, (float)screenY);
    }
}
