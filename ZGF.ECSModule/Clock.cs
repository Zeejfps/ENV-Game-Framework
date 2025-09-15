namespace ZGF.ECSModule;

public sealed class Clock
{
    public float UnscaledDeltaTime { get; private set; }
    public float ScaledDeltaTime { get; private set; }
    public float TimeScale { get; set; } = 1f;

    public void Tick(float dt)
    {
        UnscaledDeltaTime = dt;
        ScaledDeltaTime = dt * TimeScale;
    }
}