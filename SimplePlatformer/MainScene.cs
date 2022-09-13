namespace SimplePlatformer;

public class MainScene
{
    public MainScene()
    {
        
    }
    
    public static MainScene LoadAsync()
    {
        return new MainScene();
    }
}

public sealed class SpriteAnimation : IAnimation
{
    private readonly List<Sprite> m_Frames;

    public Sprite this[int frameIndex] => m_Frames[frameIndex];
    public int FrameCount { get; }

    private SpriteAnimation(List<Sprite> frames)
    {
        m_Frames = frames;
        FrameCount = m_Frames.Count;
    }

    public static SpriteAnimation Create(IEnumerable<Sprite> frames)
    {
        return new SpriteAnimation(frames.ToList());
    }
}

public interface IAnimation
{
    int FrameCount { get; }
}