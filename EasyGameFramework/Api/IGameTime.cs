namespace EasyGameFramework.Api;

public interface IGameTime
{
    float FixedUpdateDeltaTime { get; }
    float UpdateDeltaTime { get; }
    float FrameLerpFactor { get; }

    void SetFixedUpdateDeltaTime(float dt);
}