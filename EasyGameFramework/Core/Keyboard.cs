using EasyGameFramework.Api.Events;
using EasyGameFramework.Api.InputDevices;

namespace EasyGameFramework.Core;

internal class Keyboard : IKeyboard
{
    public event KeyboardKeyPressedDelegate? KeyPressed;
    
    private readonly HashSet<KeyboardKey> m_KeysPressedThisFrame = new();
    private readonly HashSet<KeyboardKey> m_KeysReleasedThisFrame = new();
    private readonly HashSet<KeyboardKey> m_PressedKeys = new();

    public void PressKey(KeyboardKey key)
    {
        // If we fail to add the key to the collection then it is already pressed
        if (!m_PressedKeys.Add(key)) 
            return;
        
        m_KeysPressedThisFrame.Add(key);
        KeyPressed?.Invoke(new KeyboardKeyPressedEvent
        {
            Keyboard = this,
            Key = key
        });
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

    public void Reset()
    {
        m_KeysPressedThisFrame.Clear();
        m_KeysReleasedThisFrame.Clear();
    }
}