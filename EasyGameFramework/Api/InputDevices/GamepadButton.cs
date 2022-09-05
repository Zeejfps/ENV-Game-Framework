namespace EasyGameFramework.Api.InputDevices;

public readonly struct InputButtonStateChangedEvent
{
    public GamepadButton Button { get; init; }
}

public delegate void InputButtonStateChangedDelegate(InputButtonStateChangedEvent evt);

public interface IInputButton
{
    event InputButtonStateChangedDelegate? Pressed;
    event InputButtonStateChangedDelegate? Released;
    event InputButtonStateChangedDelegate? StageChanged;
}

public sealed class GamepadButton : IInputButton
{
    public event InputButtonStateChangedDelegate? Pressed;
    public event InputButtonStateChangedDelegate? Released;
    public event InputButtonStateChangedDelegate? StageChanged;

    private bool m_Value;

    public bool Value
    {
        get => m_Value;
        set
        {
            if (m_Value == value)
                return;
            m_Value = value;
            if (m_Value)
                OnPressed();
            else
                OnReleased();
        }
    }

    public bool IsPressed => Value == true;
    public bool IsReleased => Value == false;

    private void OnPressed()
    {
        var evt = new InputButtonStateChangedEvent
        {
            Button = this,
        };
        
        StageChanged?.Invoke(evt);
        Pressed?.Invoke(evt);
    }

    private void OnReleased()
    {
        var evt = new InputButtonStateChangedEvent
        {
            Button = this,
        };
        
        StageChanged?.Invoke(evt);
        Released?.Invoke(evt);
    }
}