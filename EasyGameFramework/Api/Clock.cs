namespace EasyGameFramework.Api;

public class Clock : IClock
{
    public float DeltaTime { get; private set; }
    public float Time { get; private set; }

    public void Tick(float dt)
    {
        DeltaTime = dt;
        Time += DeltaTime;
    }
}