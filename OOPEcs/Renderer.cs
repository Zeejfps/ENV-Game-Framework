using EasyGameFramework.Api;
using Entities;
using OpenGL;
using OpenGLSandbox;
using Tetris;

namespace OOPEcs;

public sealed class Renderer : IEntity, 
    ITextRenderer,
    ISpriteRenderer
{
    private readonly IWindow m_Window;
    private readonly SpriteRenderer m_SpriteRenderer;
    private readonly BitmapFontTextRenderer m_TextRenderer;

    public Renderer(IWindow window)
    {
        m_Window = window;
        m_TextRenderer = new BitmapFontTextRenderer(window);
        m_SpriteRenderer = new SpriteRenderer(window);
    }

    private bool m_IsLoaded;
    
    public void Load()
    {
        if (m_IsLoaded)
            return;
        
        m_IsLoaded = true;
        m_TextRenderer.Load(new BmpFontFile
        {
            FontName = "test",
            PathToFile = "Assets/bitmapfonts/Segoe UI.fnt"
        });
        m_SpriteRenderer.Load();
        
        m_Window.Paint += Window_OnPaint;
    }

    public void Unload()
    {
        if (!m_IsLoaded)
            return;
        
        m_SpriteRenderer.Unload();
        m_TextRenderer.Unload();
        m_IsLoaded = false;
    }

    private void Window_OnPaint()
    {
        Gl.glClear(Gl.GL_COLOR_BUFFER_BIT | Gl.GL_DEPTH_BUFFER_BIT);
        //m_TextRenderer.Update();
        m_SpriteRenderer.Update();
    }

    public IRenderedText Render(string text, string fontFamily, Rect screenPosition, TextStyle style)
    {
        return m_TextRenderer.Render(text, fontFamily, screenPosition, style);
    }

    public float CalculateTextWidth(string text, string fontName)
    {
        return m_TextRenderer.CalculateTextWidth(text, fontName);
    }

    public IRenderedSprite Render(Rect screenRect)
    {
        return m_SpriteRenderer.Render(screenRect);
    }
}