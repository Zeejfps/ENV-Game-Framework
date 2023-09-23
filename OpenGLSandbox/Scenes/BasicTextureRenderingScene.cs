using EasyGameFramework.Api;
using EasyGameFramework.Api.AssetTypes;
using static GL46;
using static OpenGLSandbox.Utils_GL;

namespace OpenGLSandbox;

public unsafe class BasicTextureRenderingScene : IScene
{
    private uint m_BufferId;
    private uint m_TextureId;
    private uint m_ShaderProgramId;
    private readonly IAssetLoader<ICpuTexture> m_ImageLoader;

    public BasicTextureRenderingScene(IAssetLoader<ICpuTexture> loader)
    {
        m_ImageLoader = loader;
    }
    
    public void Load()
    {
        var image = m_ImageLoader.Load("Assets/lol");
        
        uint bufferId;
        glGenBuffers(1, &bufferId);
        AssertNoGlError();

        m_BufferId = bufferId;

        uint textureId;
        glGenTextures(1, &textureId);
        AssertNoGlError();

        m_TextureId = textureId;
        
        glBindTexture(GL_TEXTURE_2D, textureId);
        AssertNoGlError();

        var pixels = image.Pixels;
        Console.WriteLine($"{image.Width}x{image.Height}");
        Console.WriteLine(pixels.Length);
        fixed (byte* ptr = &pixels[0])
        {
            glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA8, 100, 100, 0, GL_RGBA, GL_BYTE, ptr);
            AssertNoGlError();
        }

        m_ShaderProgramId = new ShaderProgramBuilder()
            .WithVertexShader("Assets/tex.vert.glsl")
            .WithFragmentShader("Assets/tex.frag.glsl")
            .Build();
    }

    public void Render()
    {
    }

    public void Unload()
    {
        fixed (uint* ptr = &m_BufferId)
            glDeleteBuffers(1, ptr);
        
        fixed (uint* ptr = &m_TextureId)
            glDeleteTextures(1, ptr);
    }
}