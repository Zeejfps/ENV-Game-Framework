using EasyGameFramework.Api.Events;

namespace EasyGameFramework.Api.InputDevices;

public delegate void KeyboardKeyStateChangedDelegate(in KeyboardKeyStateChangedEvent evt);

public interface IKeyboard
{
    event KeyboardKeyStateChangedDelegate KeyPressed;
    event KeyboardKeyStateChangedDelegate KeyReleased;
    event KeyboardKeyStateChangedDelegate KeyStateChanged;
    
    void RepeatKey(KeyboardKey key);
    void PressKey(KeyboardKey key);
    void ReleaseKey(KeyboardKey key);
    
    bool IsKeyPressed(KeyboardKey key);
    bool IsKeyReleased(KeyboardKey key);
}