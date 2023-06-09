namespace EasyGameFramework.Api;

public interface IClock
{
    event Action Ticked;

    float Time { get; }
    float DeltaTime { get; }
    
    void Tick(float dt);
}