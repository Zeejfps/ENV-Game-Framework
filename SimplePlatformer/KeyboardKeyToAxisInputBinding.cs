using EasyGameFramework.Api.InputDevices;

namespace SimplePlatformer;

public sealed class KeyboardKeyToAxisInputBinding : IAxisInputBinding
{
    private KeyboardKey m_Key;
    private float m_ValueWhenPressed;

    public KeyboardKeyToAxisInputBinding(KeyboardKey key, float valueWhenPressed)
    {
        m_Key = key;
        m_ValueWhenPressed = valueWhenPressed;
    }

    public float Poll(IKeyboard keyboard, IMouse mouse, IGamepad gamepad)
    {
        if (keyboard.IsKeyPressed(m_Key))
            return m_ValueWhenPressed;
        return 0f;
    }
}