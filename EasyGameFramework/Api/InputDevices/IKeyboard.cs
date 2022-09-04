namespace EasyGameFramework.Api.InputDevices;

public interface IKeyboard
{
    void PressKey(KeyboardKey key);
    void ReleaseKey(KeyboardKey key);

    bool WasAnyKeyPressedThisFrame(out KeyboardKey key);

    bool WasKeyPressedThisFrame(KeyboardKey key);
    bool WasKeyReleasedThisFrame(KeyboardKey key);
    bool IsKeyPressed(KeyboardKey key);
    bool IsKeyReleased(KeyboardKey key);

    void Reset();
    
    void CreateKeyToActionBinding(KeyboardKey key, string actionName);
    void ClearKeyBinding(KeyboardKey key);

}