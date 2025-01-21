using System;
using Bricks;
using Microsoft.Xna.Framework.Input;

namespace Brickz.MonoGame;

public sealed class MonoGameKeyboard : IKeyboard
{
    private KeyboardState _oldState;

    public void Init()
    {
        _oldState = Keyboard.GetState();
    }
    
    public void Update()
    {
        _oldState = Keyboard.GetState();
    }
    
    public bool IsKeyDown(KeyCode keyCode)
    {
        var keyStates = Keyboard.GetState();
        var monoKey = ConvertKeyCodeToMonoKey(keyCode);
        return keyStates.IsKeyDown(monoKey);
    }

    public bool WasKeyPressedThisFrame(KeyCode keyCode)
    {
        var keyStates = Keyboard.GetState();
        var monoKey = ConvertKeyCodeToMonoKey(keyCode);
        return keyStates.IsKeyDown(monoKey) && !_oldState.IsKeyDown(monoKey);
    }

    private Keys ConvertKeyCodeToMonoKey(KeyCode keyCode)
    {
        return keyCode switch
        {
            KeyCode.A => Keys.A,
            KeyCode.D => Keys.D,
            KeyCode.L => Keys.L,
            KeyCode.P => Keys.P,
            KeyCode.Space => Keys.Space,
            _ => throw new ArgumentOutOfRangeException(nameof(keyCode), keyCode, null)
        };
    }
}