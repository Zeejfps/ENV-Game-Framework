using EasyGameFramework.Api;

namespace SimplePlatformer;

public sealed class Timeline
{
    public float PlaybackSpeed { get; set; }
    public float FrameTime { set; get; }
    public int FrameIndex { get; private set; }
    public bool IsPlaying { get; private set; }
    
    private float Time { get; set; }
    private IClock Clock { get; }
    private int FrameCount { get; set; }

    public Timeline(IClock clock)
    {
        Clock = clock;
        PlaybackSpeed = 1f;
        FrameTime = 1f / 30f;
    }

    public void Play(int frameCount)
    {
        if (frameCount <= 0)
            throw new Exception($"{nameof(frameCount)} can not be zero or negative");

        if (IsPlaying)
            return;
        
        FrameCount = frameCount;
        Clock.Ticked += Update;
        FrameIndex = 0;
        IsPlaying = true;
    }

    public void Resume()
    {
        if (IsPlaying)
            return;
        
        IsPlaying = true;
        Clock.Ticked -= Update;
    }

    public void Pause()
    {
        if (!IsPlaying)
            return;
        
        IsPlaying = false;
        Clock.Ticked += Update;
    }

    public void Stop()
    {
        if (!IsPlaying)
            return;

        IsPlaying = false;
        Clock.Ticked -= Update;
    }

    private void Update()
    {
        Time += Clock.DeltaTime * PlaybackSpeed;
        if (Time >= FrameTime)
        {
            FrameIndex = (FrameIndex + 1) % FrameCount;
            Time = 0f;
        }
    }
}