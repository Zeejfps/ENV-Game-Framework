using EasyGameFramework.GUI;
using OpenGLSandbox;
using Raylib_CsLo;
using Color = Raylib_CsLo.Color;

namespace Bricks.RaylibBackend;

public sealed class RaylibTextRenderer : ITextRenderer
{
    public IRenderedText Render(string text, Rect screenPosition, TextStyle style)
    {
        var renderedText = new RaylibText();
        return renderedText;
    }

    public float CalculateTextWidth(string text, string fontName)
    {
        return Raylib.MeasureText(text, 40);
    }
}

public sealed class RaylibText : IRenderedText
{
    public Rect ScreenRect { get; set; }
    public TextStyle Style { get; set; }
    public Rect Bounds { get; }
    public int GlyphCount { get; }
    public IRenderedGlyph GetGlyph(int index)
    {
        throw new NotImplementedException();
    }

    public void Render()
    {
        var screenRect = ScreenRect;
        Raylib.DrawText("asdwd", screenRect.X, screenRect.Y,40, new Color());
    }
    
    public void Dispose()
    {
        
    }
}