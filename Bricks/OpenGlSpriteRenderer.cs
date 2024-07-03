using OpenGLSandbox;
using static GL46;

namespace Bricks;

public sealed class OpenGlSpriteRenderer : ISpriteRenderer
{
    private readonly Dictionary<ITextureHandle, HashSet<ISprite>> m_SpritesByTextureHandlers = new();
    
    public OpenGlSpriteRenderer()
    {
    }
    
    public void Load()
    {
        var vertexShader = OpenGlUtils.CreateAndCompileShaderFromSourceFile(GL_VERTEX_SHADER, "Assets/Shaders/sprite.vert.glsl");
        var fragmentShader = OpenGlUtils.CreateAndCompileShaderFromSourceFile(GL_FRAGMENT_SHADER, "Assets/Shaders/sprite.frag.glsl");

        //m_InstanceRenderer.Load();
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
        //m_InstanceRenderer.Add(sprite);
    }
    

    public void Render()
    {
        // m_InstanceRenderer.Update();
        // m_InstanceRenderer.Render();
    }
}