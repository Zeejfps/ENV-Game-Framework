using Raylib_cs;

namespace Bricks.RaylibBackend;

internal sealed class RaylibKeyboard : IKeyboard
{
    public bool IsKeyDown(KeyCode keyCode)
    {
        var keyboardKey = ConvertKeyCodeToKeyboardKey(keyCode);
        return Raylib.IsKeyDown(keyboardKey);
    }

    public bool WasKeyPressedThisFrame(KeyCode keyCode)
    {
        var keyboardKey = ConvertKeyCodeToKeyboardKey(keyCode);
        return Raylib.IsKeyPressed(keyboardKey);
    }

    private KeyboardKey ConvertKeyCodeToKeyboardKey(KeyCode keyCode)
    {
        return keyCode switch
        {
            KeyCode.A => KeyboardKey.A,
            KeyCode.D => KeyboardKey.D,
            KeyCode.L => KeyboardKey.L,
            KeyCode.P => KeyboardKey.P,
            KeyCode.Space => KeyboardKey.Space,
            _ => throw new ArgumentOutOfRangeException(nameof(keyCode), keyCode, null)
        };
    }
}