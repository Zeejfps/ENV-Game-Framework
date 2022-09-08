namespace SimplePlatformer;

public sealed class ButtonInput
{
    public event Action? Pressed;
    public event Action? Released;

    private bool m_IsPressed;
    public bool IsPressed
    {
        get => m_IsPressed;
        set
        {
            if (m_IsPressed == value)
                return;
            m_IsPressed = value;
            if (m_IsPressed)
                Pressed?.Invoke();
            else
                Released?.Invoke();
        }
    }
}