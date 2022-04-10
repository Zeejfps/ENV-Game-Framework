namespace ENV.Engine.InputDevices;

public interface IKeyboard
{
    bool WasKeyPressedThisFrame(KeyboardKey key);
    bool WasKeyReleasedThisFrame(KeyboardKey key);
    bool IsKeyPressed(KeyboardKey key);
    bool IsKeyReleased(KeyboardKey key);
}

public enum KeyboardKey
{
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