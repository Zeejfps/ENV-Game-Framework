using EasyGameFramework.Api.InputDevices;

namespace EasyGameFramework.Glfw;

internal class Keyboard : IKeyboard
{
    private readonly HashSet<KeyboardKey> m_KeysPressedThisFrame = new();
    private readonly HashSet<KeyboardKey> m_KeysReleasedThisFrame = new();
    private readonly HashSet<KeyboardKey> m_PressedKeys = new();

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

    public bool WasAnyKeyPressedThisFrame(out KeyboardKey key)
    {
        if (m_KeysPressedThisFrame.Count == 0)
        {
            key = KeyboardKey.Unknown;
            return false;
        }

        key = m_KeysPressedThisFrame.First();
        return true;
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