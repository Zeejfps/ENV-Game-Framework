using EasyGameFramework.Api;

namespace EasyGameFramework.Core;

internal class GameTime : IGameTime
{
    public float Time { get; set; }
    public float FixedUpdateDeltaTime { get; set; }
    public float UpdateDeltaTime { get; set; }
    public float FrameLerpFactor { get; set; }
    
    public void SetFixedUpdateDeltaTime(float dt)
    {
        FixedUpdateDeltaTime = dt;
    }
}