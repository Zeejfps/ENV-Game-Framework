namespace EasyGameFramework.Api;

public interface IGameClock : IClock
{
    float UpdateDeltaTime { get; }
    float FrameDeltaTime { get; }
    float FrameLerpFactor { get; }
}