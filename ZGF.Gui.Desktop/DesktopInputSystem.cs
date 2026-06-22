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
    private bool _pendingExitClear;

    public InputSystem InputSystem { get; } = new();
    public Mouse Mouse { get; } = new();
    public Action? OnAnyInput { get; set; }

    /// <summary>
    /// Fires for every mouse button event on this window, before normal dispatch.
    /// Used by <see cref="ContextMenuManager"/> to detect a click on the main window
    /// (an "outside click") and close open menus on platforms whose native decorator
    /// provides no OS-level capture.
    /// </summary>
    public event Action<MouseButtonEvent>? OnMouseButtonPreview;

    public DesktopInputSystem(IWindow window, RenderedCanvasBase canvas, PointerOwnershipArbiter? arbiter = null)
    {
        _window = window;
        _canvas = canvas;
        _arbiter = arbiter;

        _window.OnKey += HandleKeyEvent;
        _window.OnMouseButton += HandleMouseButtonEvent;
        _window.OnScroll += HandleScrollEvent;
        _window.OnPointerEnter += HandleCursorEnter;
    }

    /// <summary>True when the OS cursor is within this window's client bounds.</summary>
    public bool IsCursorInsideWindow()
    {
        _window.GetCursorPosition(out var x, out var y);
        return x >= 0 && y >= 0 && x <= _window.Width && y <= _window.Height;
    }

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
            return;
        }

        var e = new MouseMoveEvent
        {
            Mouse = Mouse,
            Phase = EventPhase.Capturing,
        };
        InputSystem.SendMouseMovedEvent(ref e);
        OnAnyInput?.Invoke();
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
        OnMouseButtonPreview?.Invoke(e);

        // While a modal menu is open and this window sits behind it, the press's only role is
        // to dismiss the menu (the preview above / OS capture handles that). Our own hover path
        // is frozen at whatever was hovered when the menu opened — Update() clears hover but
        // keeps _focusQueue — so dispatching here would re-fire that stale path: reopening the
        // just-closed menu (left-click) or popping a context menu from a control the cursor
        // isn't even over (right-click). Skip local dispatch; the dismiss click is consumed.
        if (_arbiter == null || !_arbiter.IsBlockedByModal(this))
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
        InputSystem.SendKeyboardKeyEvent(ref e);
        OnAnyInput?.Invoke();
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
