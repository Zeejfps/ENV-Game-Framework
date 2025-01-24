using OpenGLSandbox;

namespace EasyGameFramework.GUI;

public struct TextStyle
{
    public string FontFamily; 
    public float FontScale;
    public TextAlignment HorizontalTextAlignment;
    public TextAlignment VerticalTextAlignment;
    public Color Color;

    public TextStyle()
    {
        FontScale = 1.0f;
    }
}