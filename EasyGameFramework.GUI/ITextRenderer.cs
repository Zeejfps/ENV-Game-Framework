using OpenGLSandbox;

namespace EasyGameFramework.GUI;

public interface ITextRenderer
{
    IRenderedText Render(string text, Rect screenPosition, TextStyle style);
    float CalculateTextWidth(string text, string fontName);
    float CalculateTextHeight(string text, float width, string fontFamily, float fontScale);
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