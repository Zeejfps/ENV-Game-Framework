using System.Numerics;
using EasyGameFramework.Api;
using OpenGLSandbox;
using static GL46;
using static OpenGLSandbox.OpenGlUtils;

namespace Bricks;

public sealed unsafe class OpenGlSpriteRenderer : ISpriteRenderer
{
    private readonly Dictionary<ITextureHandle, HashSet<ISprite>> m_SpritesByTextureHandlers = new();

    private readonly OpenGlTexturedQuadInstanceRenderer<SpriteInstanceData> m_InstanceRenderer;
    private uint m_ShaderProgram;
    private int m_ProjectionMatrixUniformLocation;
    
    public OpenGlSpriteRenderer()
    {
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
    }
    
    public void Add(ISprite sprite)
    {
        m_InstanceRenderer.Add(sprite);
    }
    
    public void Render(Matrix4x4 viewProjectionMatrix)
    {
        glUseProgram(m_ShaderProgram);
        
        var ptr = &viewProjectionMatrix.M11;
        glUniformMatrix4fv(m_ProjectionMatrixUniformLocation, 1, false, ptr);
        AssertNoGlError();
        
        m_InstanceRenderer.Update();
        m_InstanceRenderer.Render();
    }
}