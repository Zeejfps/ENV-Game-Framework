using System.Numerics;
using GLFW;

namespace NodeGraphApp;

public sealed class Mouse
{
    public Vector2 Position { get; set; }

    private readonly HashSet<MouseButton> _buttonsPressedThisFrame = new();
    private readonly HashSet<MouseButton> _buttonsReleasedThisFrame = new();
    private readonly HashSet<MouseButton> _pressedButtons = new();

    public bool WasButtonPressedThisFrame(MouseButton button)
    {
        return _buttonsPressedThisFrame.Contains(button);
    }

    public bool WasButtonReleasedThisFrame(MouseButton button)
    {
        return _buttonsReleasedThisFrame.Contains(button);
    }

    public bool IsButtonPressed(MouseButton button)
    {
        return _pressedButtons.Contains(button);
    }

    public void PressButton(MouseButton button)
    {
        _buttonsPressedThisFrame.Add(button);
        _pressedButtons.Add(button);
    }

    public void ReleaseButton(MouseButton button)
    {
        _buttonsReleasedThisFrame.Add(button);
        _pressedButtons.Remove(button);
    }

    public void Update()
    {
        _buttonsPressedThisFrame.Clear();
        _buttonsReleasedThisFrame.Clear();
    }

    public bool IsButtonReleased(MouseButton left)
    {
        return !_pressedButtons.Contains(left);
    }
}