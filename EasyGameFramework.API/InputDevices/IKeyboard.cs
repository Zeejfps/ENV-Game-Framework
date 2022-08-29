namespace EasyGameFramework.API.InputDevices;

public interface IKeyboard
{
    bool WasKeyPressedThisFrame(KeyboardKey key);
    bool WasKeyReleasedThisFrame(KeyboardKey key);
    bool IsKeyPressed(KeyboardKey key);
    bool IsKeyReleased(KeyboardKey key);
}

public enum KeyboardKey
{
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