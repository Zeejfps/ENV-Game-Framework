namespace EasyGameFramework.Api.InputDevices;

public interface IKeyboard
{
    IKeyboardKeyBindings KeyBindings { get; }
    
    void PressKey(KeyboardKey key);
    void ReleaseKey(KeyboardKey key);

    bool WasAnyKeyPressedThisFrame(out KeyboardKey key);

    bool WasKeyPressedThisFrame(KeyboardKey key);
    bool WasKeyReleasedThisFrame(KeyboardKey key);
    bool IsKeyPressed(KeyboardKey key);
    bool IsKeyReleased(KeyboardKey key);

    void Reset();
}