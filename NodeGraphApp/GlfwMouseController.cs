using System.Numerics;
using GLFW;

namespace NodeGraphApp;

public sealed class GlfwMouseController
{
    private readonly Mouse _mouse;

    private readonly MouseButtonCallback _mouseButtonCallback;
    private readonly MouseCallback _mousePositionCallback;
    private readonly MouseCallback _mouseScrollCallback;
    
    public GlfwMouseController(Window window, Mouse mouse)
    {
        _mouse = mouse;

        _mouseButtonCallback = (_, button, state, _) =>
        {
            if (state == InputState.Press)
            {
                mouse.PressButton(button);
            }
            else if (state == InputState.Release)
            {
                mouse.ReleaseButton(button);
            }
        };
        Glfw.SetMouseButtonCallback(window, _mouseButtonCallback);

        _mousePositionCallback = (_, x, y) =>
        {
            mouse.Position = new Vector2((float)x, (float)y);
        };
        Glfw.SetCursorPositionCallback(window, _mousePositionCallback);
        
        _mouseScrollCallback = (_, dx, dy) =>
        {
            _mouse.ScrollDelta = new Vector2((float)dx, (float)dy);
        };
        Glfw.SetScrollCallback(window, _mouseScrollCallback);
    }

    public void Update()
    {
        _mouse.ClearButtonsPressedThisFrame();
        _mouse.ClearButtonsReleasedThisFrame();
        _mouse.ScrollDelta = Vector2.Zero;
    }
}