namespace OpenGLSandbox;

public interface ITextRenderer
{
    IRenderedText Render(string text, string fontFamily, Rect screenPosition, TextStyle style);
    float CalculateTextWidth(string text, string fontName);
    Size CalculateSize(string text, string fontName, TextStyle style);
}

public struct Size
{
    public int Width;
    public int Height;

    public Size(int width, int height)
    {
        Width = width;
        Height = height;
    }
}