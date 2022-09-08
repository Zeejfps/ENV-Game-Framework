using EasyGameFramework.Api.InputDevices;

namespace SimplePlatformer;

public sealed class GamepadAxisToAxisInputBinding : IAxisInputBinding
{
    private readonly GamepadAxis m_Axis;
    private readonly float m_DeadZoneRadius;
    
    public GamepadAxisToAxisInputBinding(GamepadAxis axis, float deadZoneRadius)
    {
        m_Axis = axis;
        m_DeadZoneRadius = deadZoneRadius;
    }
    
    public float Poll(IKeyboard keyboard, IMouse mouse, IGamepad? gamepad)
    {
        if (gamepad == null)
            return 0f;

        var axisValue = gamepad.GetAxisValue(m_Axis);
        if (axisValue >= -m_DeadZoneRadius && axisValue <= m_DeadZoneRadius)
            return 0f;

        return axisValue;
    }
}