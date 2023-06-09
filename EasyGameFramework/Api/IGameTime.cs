namespace EasyGameFramework.Api;

public interface IGameTime
{
    float UpdateDeltaTime { get; }
    float FrameDeltaTime { get; }
    float FrameLerpFactor { get; }
}