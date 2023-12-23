namespace EasyGameFramework.Api;

public interface IClock
{
    event Action Ticked;
    float DeltaTime { get; }
}