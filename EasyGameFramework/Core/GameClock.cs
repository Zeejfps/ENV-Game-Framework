using EasyGameFramework.Api;

namespace EasyGameFramework.Core;

internal class GameClock : IGameClock
{
    public float Time { get; set; }
    public float UpdateDeltaTime { get; set; }
    public float FrameDeltaTime { get; set; }
    public float FrameLerpFactor { get; set; }
}