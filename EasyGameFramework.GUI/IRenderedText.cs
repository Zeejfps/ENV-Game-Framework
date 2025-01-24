using OpenGLSandbox;

namespace EasyGameFramework.GUI;

public interface IRenderedText : IDisposable
{
    Rect ScreenRect { get; set; }
    TextStyle Style { get; set; }
    
    Rect Bounds { get; }
    int GlyphCount { get; }
    IRenderedGlyph GetGlyph(int index);
}