namespace EasyGameFramework.Api;

public interface IGameClock
{
    float Time { get; }
    float UpdateDeltaTime { get; }
    float FrameDeltaTime { get; }
    float FrameLerpFactor { get; }
}