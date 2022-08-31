using EasyGameFramework.Api;

namespace EasyGameFramework.Core;

internal class GameClock : IClock
{
    public float Time { get; set; }
    public float DeltaTime { get; set; }
}