using System.Numerics;
using EasyGameFramework.Api;
using EasyGameFramework.Api.AssetTypes;
using OpenGLSandbox;
using static GL46;
using static OpenGLSandbox.OpenGlUtils;

namespace Bricks;

public sealed unsafe class OpenGlSpriteRenderer : ISpriteRenderer
{
    private readonly OpenGlTexturedQuadInstanceRenderer<SpriteInstanceData> m_InstanceRenderer;
    private uint m_ShaderProgram;
    private int m_ProjectionMatrixUniformLocation;
    private uint m_TextureId;
    private readonly IAssetLoader<ICpuTexture> m_TextureLoad;
    
    public OpenGlSpriteRenderer(IAssetLoader<ICpuTexture> textureLoader)
    {
        m_TextureLoad = textureLoader;
        m_InstanceRenderer = new OpenGlTexturedQuadInstanceRenderer<SpriteInstanceData>(200);
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

        var filterParam = GL_LINEAR;

        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAX_LEVEL, 0);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_S, GL_REPEAT);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_WRAP_T, GL_REPEAT);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, filterParam);
        glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, filterParam);

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
    
    public void Add(ISprite sprite)
    {
        m_InstanceRenderer.Add(sprite);
    }
    
    public void Render(Matrix4x4 viewProjectionMatrix)
    {
        glUseProgram(m_ShaderProgram);
        glBindTexture(GL_TEXTURE_2D, m_TextureId);
        
        var ptr = &viewProjectionMatrix.M11;
        glUniformMatrix4fv(m_ProjectionMatrixUniformLocation, 1, false, ptr);
        AssertNoGlError();
        
        m_InstanceRenderer.Update();
        m_InstanceRenderer.Render();
    }
}