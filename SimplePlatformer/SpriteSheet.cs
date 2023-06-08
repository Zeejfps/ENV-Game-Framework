namespace SimplePlatformer;

public sealed class SpriteSheet
{
    private readonly List<Sprite> m_Sprites;

    public Sprite this[int index] => m_Sprites[index];
    public int SpriteCount { get; }

    private SpriteSheet(List<Sprite> sprites)
    {
        m_Sprites = sprites;
        SpriteCount = m_Sprites.Count;
    }

    public static SpriteSheet Create(IEnumerable<Sprite> sprites)
    {
        return new SpriteSheet(sprites.ToList());
    }
}