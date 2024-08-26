namespace OpenGLSandbox;

public interface IRenderedText : IDisposable
{
    Rect ScreenRect { get; set; }
    TextStyle Style { get; set; }
    
    int GlyphCount { get; }
    IRenderedGlyph GetGlyph(int index);
}