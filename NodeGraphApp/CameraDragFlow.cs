using System.Numerics;
using GLFW;

namespace NodeGraphApp;

public sealed class CameraDragFlow
{
    public bool IsInProgress { get; private set; }
    
    private readonly Viewport _viewport;
    private readonly Mouse _mouse;
    private readonly Keyboard _keyboard;

    public CameraDragFlow(
        Viewport viewport,
        Mouse mouse,
        Keyboard keyboard)
    {
        _viewport = viewport;
        _mouse = mouse;
        _keyboard = keyboard;
    }

    private bool _isDragging;
    private Vector2 _prevCursorPosition;

    public void Update()
    {
        var mouse = _mouse;
        var viewport = _viewport;
        var keyboard = _keyboard;

        var canStartDragging = mouse.WasButtonPressedThisFrame(MouseButton.Left) &&
                               keyboard.IsKeyPressed(Keys.LeftAlt);
        canStartDragging |= mouse.WasButtonPressedThisFrame(MouseButton.Middle);
        canStartDragging &= viewport.ContainsScreenPoint(mouse.Position);

        if (canStartDragging)
        {
            _isDragging = true;
            _prevCursorPosition = viewport.ScreenToCameraViewPoint(mouse.Position);
            IsInProgress = true;
        }

        if (_isDragging)
        {
            if (mouse.WasButtonReleasedThisFrame(MouseButton.Left) ||
                mouse.WasButtonReleasedThisFrame(MouseButton.Middle))
            {
                _isDragging = false;
                IsInProgress = false;
                return;
            }

            var newCursorScreenPosition = viewport.ScreenToCameraViewPoint(mouse.Position);
            var delta = newCursorScreenPosition - _prevCursorPosition;
            _prevCursorPosition = newCursorScreenPosition;
            viewport.Camera.Position -= delta;
        }
    }
}