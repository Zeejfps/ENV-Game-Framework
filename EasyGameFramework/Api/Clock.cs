namespace EasyGameFramework.Api;

public class Clock : IClock
{
    public event Action? Ticked;
    
    public float DeltaTime { get; private set; }
    public float Time { get; private set; }
    public float TimeScale { get; set; } = 1f;

    public void Tick(float dt)
    {
        DeltaTime = dt * TimeScale;
        Time += DeltaTime;
        Ticked?.Invoke();
    }
}