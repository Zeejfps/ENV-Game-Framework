using Raylib_cs;

namespace Bricks.RaylibBackend;

internal sealed class RaylibInput : IInput
{
    public bool IsKeyDown(KeyCode keyCode)
    {
        var keyboardKey = ConvertKeyCodeToKeyboardKey(keyCode);
        return Raylib.IsKeyDown(keyboardKey);
    }

    private KeyboardKey ConvertKeyCodeToKeyboardKey(KeyCode keyCode)
    {
        return keyCode switch
        {
            KeyCode.A => KeyboardKey.A,
            KeyCode.D => KeyboardKey.D,
            _ => throw new ArgumentOutOfRangeException(nameof(keyCode), keyCode, null)
        };
    }
}