using EasyGameFramework.Api;
using OpenGLSandbox;
using Tetris;

namespace OOPEcs;

public sealed class MyTextRenderer : BitmapFontTextRenderer, IEntity
{
    private readonly IWindow m_Window;
    
    public MyTextRenderer(IWindow window) : base(window)
    {
        m_Window = window;
    }

    public void Load()
    {
        Load(new BmpFontFile
        {
            FontName = "test",
            PathToFile = "Assets/bitmapfonts/Segoe UI.fnt"
        });
        
        m_Window.Paint += Window_OnPaint;
    }

    private void Window_OnPaint()
    {
        Update();
    }
}