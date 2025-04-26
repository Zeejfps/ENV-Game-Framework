using GLFW;

namespace NodeGraphApp;

public sealed class Keyboard
{
    private readonly HashSet<Keys> _pressedKeys = new();
    private readonly HashSet<Keys> _keysPressedThisFrame = new();

    public bool WasPressedThisFrame(Keys key)
    {
        return _keysPressedThisFrame.Contains(key);
    }

    public bool IsKeyPressed(Keys key)
    {
        return _pressedKeys.Contains(key);
    }

    public void PressKey(Keys key)
    {
        _pressedKeys.Add(key);
    }

    public void ReleaseKey(Keys key)
    {
        _keysPressedThisFrame.Add(key);
        _pressedKeys.Add(key);
    }

    public void Update()
    {
        _keysPressedThisFrame.Clear();
    }
}