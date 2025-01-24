using EasyGameFramework.GUI;
using OpenGLSandbox;
using Raylib_CsLo;
using Color = Raylib_CsLo.Color;

namespace Bricks.RaylibBackend;

public sealed class RaylibTextRenderer(CommandBuffer commandBuffer) : ITextRenderer
{
    public IRenderedText Render(string text, Rect screenPosition, TextStyle style)
    {
        var renderedText = new RaylibText(text, commandBuffer, style, screenPosition);
        commandBuffer.Add(renderedText);
        return renderedText;
    }

    public float CalculateTextWidth(string text, string fontName)
    {
        return Raylib.MeasureText(text, 40);
    }
}

public sealed class RaylibText : IRenderedText, IRenderCommand
{
    private Rect _screenPosition;
    public Rect ScreenRect
    {
        get => _screenPosition; 
        set => _screenPosition = value;
    }
    
    private TextStyle _style;
    public TextStyle Style
    {
        get => _style;
        set => _style = value;
    }
    
    private readonly string _text;
    private readonly CommandBuffer _commandBuffer;

    private float _xPos;
    private float _yPos;
    private int _fontSize;
    private Color _color;
    

    public RaylibText(string text, CommandBuffer commandBuffer, TextStyle style, Rect screenPosition)
    {
        _text = text;
        _commandBuffer = commandBuffer;
        _style = style;
        _screenPosition = screenPosition;
        _fontSize = (int)style.FontScale;
        Refresh();
    }

    private void Refresh()
    {
        var style = _style;
        if (style.HorizontalTextAlignment == TextAlignment.Center)
        {
            var textWidth = Raylib.MeasureText(_text, _fontSize);
            var rectWidth = _screenPosition.Width;
            
            _xPos = _screenPosition.Left + (rectWidth - textWidth) * 0.5f;
        }
        else
        {
            _xPos = _screenPosition.Left;
        }

        if (style.VerticalTextAlignment == TextAlignment.Center)
        {
            var textHeight = _fontSize;
            var rectHeight = _screenPosition.Height;
            
            _yPos = _screenPosition.Bottom + (rectHeight - textHeight) * 0.5f;
        }
        else
        {
            _yPos = _screenPosition.Bottom;
        }

        var styleColor = style.Color;
        _color = new Color(
            (byte)(styleColor.R * 255),
            (byte)(styleColor.G * 255),
            (byte)(styleColor.B * 255),
            (byte)(styleColor.A * 255)
        );
    }

    public void Render()
    {
        Raylib.DrawText(_text, _xPos, _yPos,_fontSize, _color);
    }
    
    public void Dispose()
    {
        _commandBuffer.Remove(this);
    }
    
    #region Unused

    public Rect Bounds { get; }
    public int GlyphCount { get; }
    public IRenderedGlyph GetGlyph(int index)
    {
        throw new NotImplementedException();
    }

    #endregion
}