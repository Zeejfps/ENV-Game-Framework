using EasyGameFramework.Api;

namespace EasyGameFramework.Core;

internal class GameClock : IGameClock
{
    public event Action? Ticked;
    
    public float Time { get; set; }
    public float DeltaTime => UpdateDeltaTime;
    
    public float UpdateDeltaTime { get; set; }
    public float FrameDeltaTime { get; set; }
    public float FrameLerpFactor { get; set; }

    public void OnTicked()
    {
        Ticked?.Invoke();
    }
}