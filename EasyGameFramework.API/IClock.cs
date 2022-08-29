namespace EasyGameFramework.API;

public interface IClock
{
    float DeltaTime { get; }
    float Time { get; }
    void Tick();
}