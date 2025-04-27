using System.Numerics;
using GLFW;

namespace NodeGraphApp;

public sealed class CameraDragController
{
    private readonly Window _window;
    private readonly Camera _camera;
    private readonly Mouse _mouse;
    private readonly Keyboard _keyboard;

    public CameraDragController(
        Window window,
        Camera camera,
        Mouse mouse,
        Keyboard keyboard)
    {
        _window = window;
        _camera = camera;
        _mouse = mouse;
        _keyboard = keyboard;
    }

    private bool _isDragging;
    private Vector2 _prevCursorPosition;

    public void Update()
    {
        var window = _window;
        var mouse = _mouse;
        var keyboard = _keyboard;
        var camera = _camera;

        var canStartDragging = mouse.WasButtonPressedThisFrame(MouseButton.Left) &&
                               keyboard.IsKeyPressed(Keys.LeftAlt);
        canStartDragging |= mouse.WasButtonPressedThisFrame(MouseButton.Middle);

        if (canStartDragging)
        {
            _isDragging = true;
            _prevCursorPosition = CoordinateUtils.ScreenToCameraViewPoint(window, camera, mouse.Position);
        }

        if (_isDragging)
        {
            if (mouse.WasButtonReleasedThisFrame(MouseButton.Left) ||
                mouse.WasButtonReleasedThisFrame(MouseButton.Middle))
            {
                _isDragging = false;
                return;
            }

            var newCursorScreenPosition = CoordinateUtils.ScreenToCameraViewPoint(window, camera, mouse.Position);
            var delta = newCursorScreenPosition - _prevCursorPosition;
            _prevCursorPosition = newCursorScreenPosition;
            camera.Position -= delta;
        }
    }
}