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

    void Update();
}

public enum KeyboardKey
{
    None,

    Alpha1,
    Alpha2,
    Alpha3,
    Alpha4,
    A,
    B,
    C,
    D,
    E,
    F,
    G,
    H,
    I,
    J,
    K,
    Q,
    R,
    S,
    T,
    W,
    Y,

    Space,
    Escape
}