using EasyGameFramework.Api.Events;
using EasyGameFramework.Api.InputDevices;

namespace EasyGameFramework.Core;

internal class Keyboard : IKeyboard
{
    public event KeyboardKeyStateChangedDelegate? KeyPressed;
    public event KeyboardKeyStateChangedDelegate? KeyReleased;
    public event KeyboardKeyStateChangedDelegate? KeyStateChanged;

    private readonly HashSet<KeyboardKey> m_PressedKeys = new();

    public void PressKey(KeyboardKey key)
    {
        // If we fail to add the key to the collection then it is already pressed
        if (!m_PressedKeys.Add(key)) 
            return;
        OnKeyPressed(key);
    }

    public void ReleaseKey(KeyboardKey key)
    {
        if (!m_PressedKeys.Remove(key))
            return;
        OnKeyReleased(key);
    }

    public bool IsKeyPressed(KeyboardKey key)
    {
        return m_PressedKeys.Contains(key);
    }

    public bool IsKeyReleased(KeyboardKey key)
    {
        return !m_PressedKeys.Contains(key);
    }

    private void OnKeyPressed(KeyboardKey key)
    {
        var evt = new KeyboardKeyStateChangedEvent
        {
            Key = key,
            Keyboard = this,
        };
        
        KeyStateChanged?.Invoke(evt);
        KeyPressed?.Invoke(evt);
    }

    private void OnKeyReleased(KeyboardKey key)
    {
        var evt = new KeyboardKeyStateChangedEvent
        {
            Key = key,
            Keyboard = this,
        };
        
        KeyStateChanged?.Invoke(evt);
        KeyReleased?.Invoke(evt);
    }
}