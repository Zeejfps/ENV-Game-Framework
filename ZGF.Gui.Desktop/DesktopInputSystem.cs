using System.Text;
using ZGF.Desktop;
using ZGF.Desktop.Input;
using ZGF.Geometry;
using ZGF.Gui.Desktop.Input;
using ZGF.KeyboardModule;
using InputState = ZGF.Gui.Desktop.Input.InputState;
using MouseButton = ZGF.Gui.Desktop.Input.MouseButton;

namespace ZGF.Gui.Desktop;

public sealed class DesktopInputSystem : IPointerWindow, IImeHost, IImeWindow
{
    private readonly IWindow _window;
    private readonly IUiScale _uiScale;
    private readonly PointerOwnershipArbiter? _arbiter;
    private readonly IWindowedApp? _app;
    private readonly ImeCoordinator? _ime;
    private readonly WindowCoordinates _coordinates;
    private bool _pendingExitClear;

    // Where the OS cursor was at the last poll, so a poll can tell "the pointer moved" from "the
    // pointer is where it was". Null until the first reading.
    private CanvasPoint? _lastPolledPoint;

    // Where the physical cursor sat when a driver took the pointer, so a real hand can be told
    // from the stillness the driver relies on. In window coordinates, like the threshold it is
    // compared against: it is the physical mouse being measured, not anything the UI is drawn in.
    private WindowPoint? _physicalAtSuspend;

    /// <summary>How far the physical cursor must travel to take the pointer back from a driver.
    /// Small enough that anyone reaching for the mouse wins immediately, large enough that a
    /// desk bump or a sub-pixel jitter does not.</summary>
    private const float ResumeThreshold = 4f;

    /// <summary>
    /// Set while something is driving the pointer — automation, a test harness — so the physical
    /// cursor is ignored rather than dragging the driven pointer back every frame. Released as
    /// soon as the physical cursor actually travels, so a person is never locked out of their own
    /// mouse; a driver never has to remember to hand it back.
    /// </summary>
    public bool PointerDriven { get; private set; }

    /// <summary>Takes the pointer for a driver. Called when a synthetic move or click is injected.</summary>
    public void BeginDrivingPointer()
    {
        PointerDriven = true;
        _physicalAtSuspend = CursorPoint();
    }

    /// <summary>Whether a hand has moved the real mouse far enough to want the pointer back.</summary>
    private bool PhysicalPointerReclaimed()
    {
        if (_physicalAtSuspend is not { } origin) return true;

        var cursor = CursorPoint();
        if (Math.Abs(cursor.X - origin.X) <= ResumeThreshold &&
            Math.Abs(cursor.Y - origin.Y) <= ResumeThreshold)
        {
            return false;
        }

        PointerDriven = false;
        _physicalAtSuspend = null;
        return true;
    }
    // Buttons whose press was swallowed as a modal-dismiss click. The matching release is
    // part of the same gesture and must be swallowed too — dispatched alone, it would land
    // on whatever sits under the cursor now that the menu is gone.
    private readonly HashSet<MouseButton> _modalDismissButtons = new();

    public InputSystem InputSystem { get; } = new();
    public Mouse Mouse { get; } = new();
    public Action? OnAnyInput { get; set; }

    public DesktopInputSystem(
        IWindow window,
        IUiScale uiScale,
        PointerOwnershipArbiter? arbiter = null,
        IWindowedApp? app = null,
        ImeCoordinator? ime = null)
    {
        _window = window;
        _uiScale = uiScale;
        _arbiter = arbiter;
        _app = app;
        _ime = ime;
        _coordinates = new WindowCoordinates(window, uiScale);

        _window.OnKey += HandleKeyEvent;
        _window.OnText += HandleTextEvent;
        _window.OnPreedit += HandlePreeditEvent;
        _window.OnMouseButton += HandleMouseButtonEvent;
        _window.OnScroll += HandleScrollEvent;
        _window.OnFocusChanged += HandleFocusChanged;
        _window.OnPointerEnter += HandleCursorEnter;

        InputSystem.ImeHost = this;
    }

    // IImeHost — what a field in this window reports. It says only that it is editing and where its
    // caret is; the coordinator decides which window that makes compose, because the answer is not
    // always this one (a menu's search box composes against the host window that holds OS focus).
    public void SetImeEnabled(bool enabled) => _ime?.SetFieldEditing(this, enabled);

    public void SetImeCaretRect(RectF caretRect) => _ime?.SetFieldCaret(this, caretRect);

    public void ResetComposition() => _ime?.ResetComposition();

    // IImeWindow — the native switches, driven from the coordinator.
    public bool HasKeyboardFocus => InputSystem.HasFocus;

    public void SetTextInputFocus(bool focused) => _window.SetTextInputFocus(focused);

    public void ResetImeComposition() => _window.ResetPreedit();

    public RectI CanvasToScreen(RectF canvasRect) =>
        _coordinates.ToScreenPoints(CanvasRect.From(canvasRect)).Points;

    /// <summary>Rebases a screen-space caret into this window's client area, which is what the IME
    /// wants. The rect can come from another window's canvas, so screen space is where it arrives.</summary>
    public void SetImeCursorRect(RectI screenRect)
    {
        _window.GetPosition(out var windowX, out var windowY);
        _window.SetPreeditCursorRect(
            screenRect.X - windowX,
            screenRect.Y - windowY,
            Math.Max(1, screenRect.Width),
            Math.Max(1, screenRect.Height));
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
        var cursor = CursorPoint();
        _window.GetFrameSize(out var left, out var top, out var right, out var bottom);
        return cursor.X >= -left && cursor.Y >= -top &&
               cursor.X <= _window.Width + right && cursor.Y <= _window.Height + bottom;
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
        // InputSystem.Reset drops the focused component without raising OnFocusLost, so a text field
        // that was editing when its popup closed never gets to end its own edit session. Left in the
        // coordinator's editing set, this (pooled, hidden) window would keep asserting the IME.
        _ime?.SetFieldEditing(this, false);
        _pendingExitClear = false;
        _modalDismissButtons.Clear();
        Mouse.Point = OffScreen;
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
                Mouse.Point = OffScreen;
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
            Mouse.Point = OffScreen;
            return;
        }

        if (PointerDriven && !PhysicalPointerReclaimed())
        {
            // A driver owns the pointer: every branch below reads the physical cursor, and one of
            // them reports it leaving the window — which is true of the real mouse and false of
            // the driven one, and would take back a hover the driver is waiting on. Hover still
            // refreshes, from wherever the driver put the pointer.
            InputSystem.RefreshHover(Mouse);
            _window.SetCursor(InputSystem.DesiredCursor);
            return;
        }

        if (InputSystem.HasFocus)
        {
            var capturedPoint = ToCanvas(CursorPoint());
            if (capturedPoint.Points != Mouse.Point)
            {
                Mouse.Point = capturedPoint.Points;
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
            Mouse.Point = OffScreen;
            return;
        }

        if (_pendingExitClear)
        {
            _pendingExitClear = false;
            var prev = Mouse.Point;
            Mouse.Point = OffScreen;
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
        var cursor = CursorPoint();
        var cursorInsideBounds =
            cursor.X >= 0 && cursor.Y >= 0 &&
            cursor.X <= _window.Width && cursor.Y <= _window.Height;

        if (!cursorInsideBounds)
        {
            return;
        }

        var guiPoint = ToCanvas(cursor);

        // A poll that finds the physical cursor where it left it says nothing about the pointer.
        // Overwriting Mouse.Point anyway is what made injected positions impossible to use: a
        // driven move survived a single frame before being dragged back to the physical cursor,
        // and the drag back was itself dispatched as a move. Hover, dwell and cursor shape all
        // follow Mouse.Point, so whoever set it last — the OS or a driver — has to keep it.
        var physicalMoved = _lastPolledPoint is not { } last || last != guiPoint;
        _lastPolledPoint = guiPoint;

        var prevPoint = Mouse.Point;
        if (!physicalMoved)
        {
            InputSystem.RefreshHover(Mouse);
        }
        else if (prevPoint == guiPoint.Points)
        {
            Mouse.Point = guiPoint.Points;
            InputSystem.RefreshHover(Mouse);
        }
        else
        {
            Mouse.Point = guiPoint.Points;
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
        // Read while still inside GLFW's callback: the NSEvent this came from is the application's
        // currentEvent only for the duration of the dispatch that invoked us.
        var (gesturePhase, momentumPhase) = MacScrollPhase.Read();

        var e = new MouseWheelScrolledEvent
        {
            Mouse = Mouse,
            DeltaX = (float)x,
            DeltaY = (float)y,
            Phase = EventPhase.Capturing,
            GesturePhase = gesturePhase,
            MomentumPhase = momentumPhase,
            Modifiers = InputSystem.Modifiers,
        };
        InputSystem.SendMouseScrollEvent(ref e);
        OnAnyInput?.Invoke();
    }

    private void HandleMouseButtonEvent(int buttonIndex, InputAction action, KeyModifiers modifiers)
    {
        InputSystem.Modifiers = (InputModifiers)modifiers;

        Mouse.Point = ToCanvas(CursorPoint()).Points;
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

    private void HandleFocusChanged(bool focused)
    {
        if (!focused) InputSystem.Modifiers = InputModifiers.None;
    }

    private void HandleKeyEvent(KeyboardKey key, InputAction action, KeyModifiers mods)
    {
        InputSystem.Modifiers = (InputModifiers)mods;

        var e = new KeyboardKeyEvent
        {
            Key = key,
            State = MapState(action),
            Modifiers = (InputModifiers)mods,
            Phase = EventPhase.Capturing
        };

        if (TypingMenu() is { } menu)
        {
            menu.InputSystem.SendKeyboardKeyEvent(ref e);
            OnAnyInput?.Invoke();
            return;
        }

        InputSystem.SendKeyboardKeyEvent(ref e);
        OnAnyInput?.Invoke();
    }

    /// <summary>
    /// The open context menu that typing belongs to instead of this window, or null when this window
    /// keeps its own. A searchable menu holds keyboard focus but, on Windows, no OS focus (borderless
    /// popups are WS_EX_NOACTIVATE), so its keys, characters and compositions all arrive here and have
    /// to be handed on — and the host window's shortcuts must stay dormant meanwhile. A plain menu
    /// focuses nothing and so takes none of them. Null too when the menu window is the one receiving
    /// the event (it took focus, as on macOS): it is the typing target, and dispatches locally.
    /// </summary>
    private DesktopInputSystem? TypingMenu() =>
        _ime?.FocusedModal() is DesktopInputSystem menu && !ReferenceEquals(menu, this) ? menu : null;

    private void HandleTextEvent(uint codePoint)
    {
        if (!Rune.TryCreate(codePoint, out var rune))
            return;

        var e = new TextInputEvent
        {
            Rune = rune,
            Phase = EventPhase.Capturing,
        };

        if (TypingMenu() is { } menu)
        {
            menu.InputSystem.SendTextInputEvent(ref e);
            OnAnyInput?.Invoke();
            return;
        }

        InputSystem.SendTextInputEvent(ref e);
        OnAnyInput?.Invoke();
    }

    private void HandlePreeditEvent(PreeditText preedit)
    {
        var e = new CompositionEvent
        {
            Preedit = preedit,
            Phase = EventPhase.Capturing,
        };

        // Same hand-off as HandleTextEvent: a menu that owns typing owns the composition that
        // produces it too, or the preedit would render in the host window's field while its
        // committed text lands in the menu's. The IME is enabled on this window (the OS-focused one)
        // precisely because the menu's field is editing — see ImeCoordinator.
        if (TypingMenu() is { } menu)
        {
            menu.InputSystem.SendCompositionEvent(ref e);
            OnAnyInput?.Invoke();
            return;
        }

        InputSystem.SendCompositionEvent(ref e);
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

    // Where the pointer sits when it is nowhere: far enough outside any canvas that nothing hovers.
    private static readonly PointF OffScreen = new(float.MinValue, float.MinValue);

    private WindowPoint CursorPoint()
    {
        _window.GetCursorPosition(out var x, out var y);
        return new WindowPoint((float)x, (float)y);
    }

    private CanvasPoint ToCanvas(WindowPoint point) =>
        WindowSpace.Of(_window, _window.ContentScale * _uiScale.Value).ToCanvas(point);
}
