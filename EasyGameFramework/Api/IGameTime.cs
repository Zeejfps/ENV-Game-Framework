namespace EasyGameFramework.Api;

public interface IGameTime
{
    float UpdateDeltaTime { get; set; }
    float FrameDeltaTime { get; }
    float FrameLerpFactor { get; }
}