namespace OpenGLSandbox;

public interface ITextRenderer
{
    IRenderedText Render(string value, Rect screenPosition, TextStyle style);
}