namespace SimplePlatformer;

public sealed class AxisInput
{
    public event Action<float> ValueChanged;

    private float m_Value;

    public float Value
    {
        get => m_Value;
        set
        {
            if (Math.Abs(m_Value - value) < 0.0001f)
                return;

            m_Value = value;
            ValueChanged?.Invoke(m_Value);
        }
    }
}