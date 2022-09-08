using EasyGameFramework.Api.InputDevices;

namespace SimplePlatformer;

public class KeyboardKeyToButtonInputBinding : IButtonInputBinding
{
    private readonly KeyboardKey m_Key;

    public KeyboardKeyToButtonInputBinding(KeyboardKey key)
    {
        m_Key = key;
    }

    public bool Poll(IKeyboard keyboard, IMouse mouse, IGamepad? gamepad)
    {
        return keyboard.IsKeyPressed(m_Key);
    }
}