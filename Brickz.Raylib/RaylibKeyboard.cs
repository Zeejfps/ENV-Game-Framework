using EasyGameFramework.Api.InputDevices;
using Raylib_CsLo;
using KeyboardKey = Raylib_CsLo.KeyboardKey;

namespace Bricks.RaylibBackend;

public sealed class RaylibKeyboard : IKeyboard, EasyGameFramework.Api.InputDevices.IKeyboard
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

    public event KeyboardKeyStateChangedDelegate? KeyPressed;
    public event KeyboardKeyStateChangedDelegate? KeyReleased;
    public event KeyboardKeyStateChangedDelegate? KeyStateChanged;
    
    public void RepeatKey(EasyGameFramework.Api.InputDevices.KeyboardKey key)
    {
        throw new NotImplementedException();
    }

    public void PressKey(EasyGameFramework.Api.InputDevices.KeyboardKey key)
    {
        throw new NotImplementedException();
    }

    public void ReleaseKey(EasyGameFramework.Api.InputDevices.KeyboardKey key)
    {
        throw new NotImplementedException();
    }

    public bool IsKeyPressed(EasyGameFramework.Api.InputDevices.KeyboardKey key)
    {
        throw new NotImplementedException();
    }

    public bool IsKeyReleased(EasyGameFramework.Api.InputDevices.KeyboardKey key)
    {
        throw new NotImplementedException();
    }

    public void Update()
    {
        
    }
}