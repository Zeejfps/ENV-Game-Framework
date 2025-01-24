using OpenGLSandbox;

namespace EasyGameFramework.GUI;

public interface ITextRenderer
{
    IRenderedText Render(string text, Rect screenPosition, TextStyle style);
    float CalculateTextWidth(string text, string fontName);
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