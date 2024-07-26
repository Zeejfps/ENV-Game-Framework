using System.Numerics;
using EasyGameFramework.Api;
using EasyGameFramework.Api.AssetTypes;
using OpenGLSandbox;
using static GL46;
using static OpenGLSandbox.OpenGlUtils;

namespace Bricks;

public sealed unsafe class OpenGlSpriteRenderer : ISpriteRenderer
{
    private readonly OpenGlTexturedQuadInstanceRenderer<Sprite> m_InstanceRenderer;
    private uint m_ShaderProgram;
    private int m_ProjectionMatrixUniformLocation;
    private uint m_TextureId;
    private readonly IAssetLoader<ICpuTexture> m_TextureLoad;
    
    public OpenGlSpriteRenderer(IAssetLoader<ICpuTexture> textureLoader)
    {
        m_TextureLoad = textureLoader;
        m_InstanceRenderer = new OpenGlTexturedQuadInstanceRenderer<Sprite>(200);
    }
    
    public void Load()
    {
        m_ShaderProgram = new ShaderProgramBuilder()
            .WithVertexShader("Assets/Shaders/sprite.vert.glsl")
            .WithFragmentShader("Assets/Shaders/sprite.frag.glsl")
            .Build();
        
        m_InstanceRenderer.Load();
        
        var bytes = "projection_matrix"u8.ToArray();
        fixed(byte* ptr = &bytes[0])
            m_ProjectionMatrixUniformLocation = glGetUniformLocation(m_ShaderProgram, ptr);
        AssertNoGlError();
        
        m_TextureId = glGenTexture();
        glBindTexture(GL_TEXTURE_2D, m_TextureId);
        AssertNoGlError();

        var filterParam = GL_NEAREST;

        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAX_LEVEL, 0);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, (int)GL_REPEAT);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, (int)GL_REPEAT);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, (int)filterParam);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, (int)filterParam);

        var texture = m_TextureLoad.Load("Assets/sprite_atlas");
        var width = texture.Width;
        var height = texture.Height;
        var pixels = texture.Pixels;
        fixed (byte* p = &pixels[0])
        {
            glCompressedTexImage2D(GL_TEXTURE_2D, 0, GL_COMPRESSED_RGBA_BPTC_UNORM_ARB
                , width, height, 0,
                width * height, p);
        }
        AssertNoGlError();
    }
    
    public void Add(IEntity<Sprite> sprite)
    {
        m_InstanceRenderer.Add(sprite);
    }
    
    public void Render(Matrix4x4 viewProjectionMatrix)
    {
        glEnable(GL_BLEND);
        glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
            
        glUseProgram(m_ShaderProgram);
        glBindTexture(GL_TEXTURE_2D, m_TextureId);
        
        var ptr = &viewProjectionMatrix.M11;
        glUniformMatrix4fv(m_ProjectionMatrixUniformLocation, 1, false, ptr);
        AssertNoGlError();
        
        m_InstanceRenderer.Render();
    }
}