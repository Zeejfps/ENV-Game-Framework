using EasyGameFramework.Api;
using OpenGL;
using OpenGLSandbox;
using Tetris;

namespace OOPEcs;

public sealed class Renderer : IEntity, ITextRenderer
{
    private readonly IWindow m_Window;

    private readonly BitmapFontTextRenderer m_TextRenderer;
    
    public Renderer(IWindow window)
    {
        m_Window = window;
        m_TextRenderer = new BitmapFontTextRenderer(window);
    }

    public void Load()
    {
        m_TextRenderer.Load(new BmpFontFile
        {
            FontName = "test",
            PathToFile = "Assets/bitmapfonts/Segoe UI.fnt"
        });
        
        m_Window.Paint += Window_OnPaint;
    }

    public void Unload()
    {
        m_TextRenderer.Unload();
    }

    private void Window_OnPaint()
    {
        Gl.glClear(Gl.GL_COLOR_BUFFER_BIT | Gl.GL_DEPTH_BUFFER_BIT);
        m_TextRenderer.Update();
    }

    public IRenderedText Render(string value, Rect screenPosition, TextStyle style)
    {
        return m_TextRenderer.Render(value, screenPosition, style);
    }
}