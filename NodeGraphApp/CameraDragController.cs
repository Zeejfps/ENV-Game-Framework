using System.Numerics;
using GLFW;

public sealed class CameraDragController
{
    private readonly Window _window;
    private readonly Camera _camera;

    public CameraDragController(Window window, Camera camera)
    {
        _window = window;
        _camera = camera;
    }

    private bool _isDragging;
    private Vector2 _prevCursorPosition;

    public void Enable()
    {
        var window = _window;
        Glfw.SetMouseButtonCallback(window, (_, button, state, modifiers) =>
        {
            if (modifiers == ModifierKeys.Alt && button == MouseButton.Left && state == InputState.Press)
            {
                _isDragging = true;
                Glfw.GetCursorPosition(window, out var posX, out var posY);
                _prevCursorPosition = new Vector2((float)posX, (float)posY);
            }
            else if (_isDragging && button == MouseButton.Left && state == InputState.Release)
            {
                _isDragging = false;
            }
        });

        Glfw.SetCursorPositionCallback(window, (_, posX, posY) =>
        {
            if (_isDragging)
            {
                Glfw.GetFramebufferSize(window, out var windowWidth, out var windowHeight);

                var camera = _camera;
                var newCursorScreenPosition = new Vector2((float)posX, (float)posY);
                var delta = newCursorScreenPosition - _prevCursorPosition;
                _prevCursorPosition = newCursorScreenPosition;

                Matrix4x4.Invert(camera.ProjectionMatrix, out var invProj);
                var ndcCoords = new Vector4
                {
                    X = -2f * delta.X / windowWidth,
                    Y = 2f * delta.Y / windowHeight,
                    Z = 0,
                    W = 0
                };

                var worldDelta = Vector4.Transform(ndcCoords, invProj);
                var cameraDelta = new Vector2(worldDelta.X, worldDelta.Y);
                camera.Position += cameraDelta;
            }
        });
    }

    public void Disable()
    {
        var window = _window;
        Glfw.SetCursorPositionCallback(window, null);
        Glfw.SetMouseButtonCallback(window, null);
    }
}