using Raylib_CsLo;

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
            KeyCode.A => KeyboardKey.KEY_A,
            KeyCode.D => KeyboardKey.KEY_D,
            KeyCode.L => KeyboardKey.KEY_L,
            KeyCode.P => KeyboardKey.KEY_P,
            KeyCode.Space => KeyboardKey.KEY_SPACE,
            _ => throw new ArgumentOutOfRangeException(nameof(keyCode), keyCode, null)
        };
    }
}