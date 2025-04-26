using System.Numerics;
using GLFW;
using NodeGraphApp;

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

        var canStartDragging = mouse.WasButtonPressedThisFrame(MouseButton.Left) &&
                               keyboard.IsKeyPressed(Keys.LeftAlt);
        canStartDragging |= mouse.WasButtonPressedThisFrame(MouseButton.Middle);

        if (canStartDragging)
        {
            _isDragging = true;
            _prevCursorPosition = mouse.Position;
        }

        if (_isDragging)
        {
            if (mouse.WasButtonReleasedThisFrame(MouseButton.Left) ||
                mouse.WasButtonReleasedThisFrame(MouseButton.Middle))
            {
                _isDragging = false;
                return;
            }

            Glfw.GetWindowSize(window, out var windowWidth, out var windowHeight);

            var camera = _camera;
            var newCursorScreenPosition = mouse.Position;
            var delta = newCursorScreenPosition - _prevCursorPosition;
            _prevCursorPosition = newCursorScreenPosition;

            var ndcCoords = new Vector4
            {
                X = -delta.X / windowWidth,
                Y = delta.Y / windowHeight,
                Z = 0,
                W = 0
            };

            Matrix4x4.Invert(camera.ProjectionMatrix, out var invProj);

            var worldDelta = Vector4.Transform(ndcCoords, invProj);
            var cameraDelta = new Vector2(worldDelta.X, worldDelta.Y);
            camera.Position += cameraDelta * 2f;
        }
    }
}