namespace EasyGameFramework.Api;

public interface IClock
{
    float Time { get; }
    float DeltaTime { get; }
}