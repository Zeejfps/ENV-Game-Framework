using static GL46;
using static OpenGLSandbox.Utils_GL;
using BmFont;

namespace OpenGLSandbox;

public interface ITextRenderer
{
    void RenderText(int x, int y, string text);
}

public sealed unsafe class TextRenderer : ITextRenderer, IDisposable
{
    private uint m_Vao;
    private uint m_Vbo;
    private uint m_Tex;
    private uint m_ShaderProgram;
    
    public TextRenderer()
    {
        uint vao;
        glGenVertexArrays(1, &vao);
        AssertNoGlError();
        m_Vao = vao;

        uint vbo;
        glGenBuffers(1, &vbo);
        AssertNoGlError();
        m_Vbo = vbo;

        uint tex;
        glGenTextures(1, &tex);
        AssertNoGlError();
        m_Tex = tex;
        
        glBindVertexArray(vao);
        AssertNoGlError();
        
        glBindBuffer(GL_ARRAY_BUFFER, vbo);
        AssertNoGlError();

        var texturedQuad = new TexturedQuad();
        glBufferData(GL_ARRAY_BUFFER, new IntPtr(sizeof(TexturedQuad)), &texturedQuad, GL_STATIC_DRAW);
        AssertNoGlError();
        
        glVertexAttribPointer(0, 2, GL_FLOAT, false, sizeof(TexturedQuad.Vertex), Offset(0));
        AssertNoGlError();
        glEnableVertexAttribArray(0);
        AssertNoGlError();
        
        glVertexAttribPointer(1, 2, GL_FLOAT, false, sizeof(TexturedQuad.Vertex), Offset<TexturedQuad.Vertex>(nameof(TexturedQuad.Vertex.TexCoords)));
        AssertNoGlError();
        glEnableVertexAttribArray(1);
        AssertNoGlError();

        m_ShaderProgram = new ShaderProgramBuilder()
            .WithVertexShader("Assets/normals.vert.glsl")
            .WithFragmentShader("Assets/color.frag.glsl")
            .Build();

        glUseProgram(m_ShaderProgram);
        AssertNoGlError();
        
        glBindTexture(GL_TEXTURE_2D, tex);
        AssertNoGlError();
    }
    
    public void RenderText(int x, int y, string helloWorld)
    {
        glDrawArrays(GL_TRIANGLES, 0, 6);
    }

    public void Dispose()
    {
        fixed (uint* vao = &m_Vao)
            glDeleteVertexArrays(1, vao);
        
        fixed (uint* vbo = &m_Vbo)
            glDeleteBuffers(1, vbo);
        
        fixed (uint* tex = &m_Tex)
            glDeleteTextures(1, tex);
    }
} 

public sealed class BitmapFontRenderingScene : IScene
{
    private ITextRenderer TextRenderer { get; set; }
    
    public void Load()
    {
        TextRenderer = new TextRenderer();
        var font = FontLoader.Load("Assets/bitmapfonts/test.fnt");
        Console.WriteLine(font.Chars[10].ID);
    }

    public void Render()
    {
        glClear(GL_COLOR_BUFFER_BIT);
        TextRenderer.RenderText(0, 0, "Hello World!");
        glFlush();
    }

    public void Unload()
    {
    }
}