namespace EasyGameFramework.Api;

public interface IClock
{
    float DeltaTime { get; }
    float Time { get; }
    void Tick(float dt);
}