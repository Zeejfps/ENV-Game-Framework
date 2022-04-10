using ENV.Engine.InputDevices;

namespace ENV.GLFW.NET;

class Keyboard_GLFW : IKeyboard
{
    private readonly HashSet<KeyboardKey> m_PressedKeys = new();
    private readonly HashSet<KeyboardKey> m_KeysPressedThisFrame = new();
    private readonly HashSet<KeyboardKey> m_KeysReleasedThisFrame = new();
    
    public void PressKey(KeyboardKey key)
    {
        if (m_PressedKeys.Add(key))
            m_KeysPressedThisFrame.Add(key);
    }

    public void ReleaseKey(KeyboardKey key)
    {
        if (m_PressedKeys.Remove(key))
            m_KeysReleasedThisFrame.Add(key);
    }
    
    public bool WasKeyPressedThisFrame(KeyboardKey key)
    {
        return m_KeysPressedThisFrame.Contains(key);
    }

    public bool WasKeyReleasedThisFrame(KeyboardKey key)
    {
        return m_KeysReleasedThisFrame.Contains(key);
    }

    public bool IsKeyPressed(KeyboardKey key)
    {
        return m_PressedKeys.Contains(key);
    }

    public bool IsKeyReleased(KeyboardKey key)
    {
        return !m_PressedKeys.Contains(key);
    }

    public void Update()
    {
        m_KeysPressedThisFrame.Clear();
        m_KeysReleasedThisFrame.Clear();
    }
}