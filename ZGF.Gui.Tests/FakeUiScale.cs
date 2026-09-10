using ZGF.Gui.Desktop;

namespace ZGF.Gui.Tests;

/// <summary>A UI scale a test can turn, standing in for the app's persisted setting.</summary>
internal sealed class FakeUiScale : IUiScale
{
    private float _value;

    public FakeUiScale(float value = 1f)
    {
        _value = value;
    }

    public float Value
    {
        get => _value;
        set
        {
            if (Math.Abs(_value - value) < float.Epsilon) return;
            _value = value;
            Changed?.Invoke(value);
        }
    }

    public event Action<float>? Changed;
}
