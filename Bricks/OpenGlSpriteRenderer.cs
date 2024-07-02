using OpenGLSandbox;

namespace Bricks;

struct SpriteData
{
    
}

class SpriteInstance : IInstancedItem<SpriteData>
{
    public event Action<IInstancedItem<SpriteData>>? BecameDirty;

    private readonly ISprite m_Sprite;

    public SpriteInstance(ISprite sprite)
    {
        m_Sprite = sprite;
    }

    public void Update(ref SpriteData instancedData)
    {
        
    }
}

public sealed class OpenGlSpriteRenderer : ISpriteRenderer
{
    private readonly Dictionary<ITextureHandle, HashSet<ISprite>> m_SpritesByTextureHandlers = new();
    private readonly OpenGlTexturedQuadInstanceRenderer<SpriteData> m_InstanceRenderer;
    
    public OpenGlSpriteRenderer()
    {
        m_InstanceRenderer = new OpenGlTexturedQuadInstanceRenderer<SpriteData>(500);
    }
    
    public void Setup()
    {
        m_InstanceRenderer.Load();
    }
    
    public void Add(ISprite sprite)
    {
        var texture = sprite.Texture;
        if (!m_SpritesByTextureHandlers.TryGetValue(texture, out var sprites))
        {
            sprites = new HashSet<ISprite>();
            m_SpritesByTextureHandlers[texture] = sprites;
        }
        sprites.Add(sprite);
        m_InstanceRenderer.Add(new SpriteInstance(sprite));
    }
    

    public void Render()
    {
        m_InstanceRenderer.Update();
        m_InstanceRenderer.Render();
    }
}