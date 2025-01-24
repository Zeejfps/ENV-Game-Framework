using OpenGLSandbox;

namespace EasyGameFramework.GUI;

public sealed class TextWidget : Widget
{
    public string Text { get; }

    private TextStyle _textStyle;
    public TextStyle Style
    {
        get => _textStyle;
        set
        {
            _textStyle = value;
            if (m_RenderedText != null)
                m_RenderedText.Style = _textStyle;
        }
    }
        
    private IRenderedText? m_RenderedText;

    public TextWidget(string text)
    {
        Text = text;
    }
        
    protected override IWidget Build(IBuildContext context)
    {
        return this;
    }

    public override Rect Measure(IBuildContext context)
    {
        var textRenderer = context.TextRenderer;
        var width = textRenderer.CalculateTextWidth(Text, Style.FontFamily);
        var height = textRenderer.CalculateTextHeight(Text, width, Style.FontFamily, Style.FontScale);
        return new Rect(0, 0, width, height);
    }

    public override void DoLayout(IBuildContext context)
    {
        var renderer = context.TextRenderer;
        m_RenderedText = renderer.Render(Text, ScreenRect, Style);
    }

    public override void Dispose()
    {
        m_RenderedText?.Dispose();
    }
}