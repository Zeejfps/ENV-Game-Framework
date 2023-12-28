namespace OpenGLSandbox;

public sealed class TextWidget : Widget
{
    public string Text { get; }
    public string FontFamily { get; init; }
    public TextStyle Style { get; init; }
        
    private IRenderedText? m_RenderedText;

    public TextWidget(string text)
    {
        Text = text;
    }
        
    protected override IWidget Build(IBuildContext context)
    {
        var renderer = context.Get<ITextRenderer>();
        m_RenderedText = renderer.Render(Text, FontFamily, ScreenRect, Style);
        return this;
    }

    public override void Dispose()
    {
        m_RenderedText?.Dispose();
    }
}