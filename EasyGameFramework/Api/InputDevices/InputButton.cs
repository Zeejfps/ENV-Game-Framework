namespace EasyGameFramework.Api.InputDevices;

public readonly struct InputButtonStateChangedEvent
{
    public InputButton Button { get; init; }
}

public delegate void InputButtonStateChangedDelegate(InputButtonStateChangedEvent evt);

public sealed class InputButton
{
    public event InputButtonStateChangedDelegate? Pressed;
    public event InputButtonStateChangedDelegate? Released;

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
        Pressed?.Invoke(new InputButtonStateChangedEvent
        {
            Button = this,
        });
    }

    private void OnReleased()
    {
        Released?.Invoke(new InputButtonStateChangedEvent
        {
            Button = this,
        });
    }
}