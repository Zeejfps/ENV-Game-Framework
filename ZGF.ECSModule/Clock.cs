namespace ZGF.ECSModule;

public sealed class Clock
{
    public float UnscaledDeltaTime { get; private set; }
    public float ScaledDeltaTime { get; private set; }
    public float TimeScale { get; set; } = 1f;
    public float FixedDeltaTime { get; set; } = 1f / 60f;
    public float ScaledFixedDeltaTime => FixedDeltaTime * TimeScale;
    public float LerpFactor { get; private set; }

    public void Tick(float dt, float lerpFactor)
    {
        UnscaledDeltaTime = dt;
        ScaledDeltaTime = dt * TimeScale;
        LerpFactor = lerpFactor;
    }
}