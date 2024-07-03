using OpenGLSandbox;

namespace Bricks;

public sealed class OpenGlSpriteRenderer : ISpriteRenderer
{
    private readonly Dictionary<ITextureHandle, HashSet<ISprite>> m_SpritesByTextureHandlers = new();
    private readonly OpenGlTexturedQuadInstanceRenderer<SpriteInstanceData> m_InstanceRenderer;
    
    public OpenGlSpriteRenderer()
    {
        m_InstanceRenderer = new OpenGlTexturedQuadInstanceRenderer<SpriteInstanceData>(500);
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
        m_InstanceRenderer.Add(sprite);
    }
    

    public void Render()
    {
        m_InstanceRenderer.Update();
        m_InstanceRenderer.Render();
    }
}