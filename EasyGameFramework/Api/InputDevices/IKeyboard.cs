using EasyGameFramework.Api.Events;

namespace EasyGameFramework.Api.InputDevices;

public delegate void KeyboardKeyPressedDelegate(in KeyboardKeyPressedEvent evt);

public interface IKeyboard
{
    event KeyboardKeyPressedDelegate KeyPressed;
    
    void PressKey(KeyboardKey key);
    void ReleaseKey(KeyboardKey key);

    bool WasAnyKeyPressedThisFrame(out KeyboardKey key);

    bool WasKeyPressedThisFrame(KeyboardKey key);
    bool WasKeyReleasedThisFrame(KeyboardKey key);
    bool IsKeyPressed(KeyboardKey key);
    bool IsKeyReleased(KeyboardKey key);

    void Reset();
}