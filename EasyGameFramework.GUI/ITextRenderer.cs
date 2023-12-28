namespace OpenGLSandbox;

public interface ITextRenderer
{
    IRenderedText Render(string text, Rect screenPosition, TextStyle style);
    float CalculateTextWidth(string text, string fontName);
}