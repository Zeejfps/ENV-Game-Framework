using OpenGLSandbox;

namespace Bricks;

using static GL46;
using static Utils_GL;

public sealed class OpenGlSpriteRenderer : ISpriteRenderer
{
    private readonly Dictionary<ITextureHandle, HashSet<ISprite>> m_SpritesByTextureHandlers = new();

    private uint m_Vbo;
    private uint m_Vao;
    
    public void Setup()
    {
        unsafe
        {
            uint vao;
            glGenVertexArrays(1, &vao);
            AssertNoGlError();
            m_Vao = vao;
            
            uint vbo;
            glGenBuffers(1, &vbo);
            AssertNoGlError();
            m_Vbo = vbo;
            
            glBindVertexArray(vao);
            AssertNoGlError();
        
            glBindBuffer(GL_ARRAY_BUFFER, vbo);
            AssertNoGlError();
        }
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
    }
    

    public void Render()
    {
        
    }
}