namespace OpenGLSandbox;

public interface ITextRenderer
{
    IRenderedText Render(string text, string fontFamily, Rect screenPosition, TextStyle style);
    float CalculateTextWidth(string text, string fontName);
}