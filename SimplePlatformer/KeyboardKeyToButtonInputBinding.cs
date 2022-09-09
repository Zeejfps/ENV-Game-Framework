using EasyGameFramework.Api.InputDevices;

namespace SimplePlatformer;

public class KeyboardKeyToButtonInputBinding : IButtonInputBinding
{
    private readonly KeyboardKey m_Key;

    public KeyboardKeyToButtonInputBinding(KeyboardKey key)
    {
        m_Key = key;
    }

    public bool Poll(Controller controller)
    {
        var keyboard = controller.Keyboard;
        if (keyboard == null)
            return false;
        return keyboard.IsKeyPressed(m_Key);
    }
}