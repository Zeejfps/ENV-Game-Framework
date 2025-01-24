namespace EasyGameFramework.GUI;

public sealed class TextWidget : Widget
{
    public string Text { get; }
    public TextStyle Style { get; init; }
        
    private IRenderedText? m_RenderedText;

    public TextWidget(string text)
    {
        Text = text;
    }
        
    protected override IWidget Build(IBuildContext context)
    {
        var renderer = context.TextRenderer;
        m_RenderedText = renderer.Render(Text, ScreenRect, Style);
        return this;
    }

    public override void Dispose()
    {
        m_RenderedText?.Dispose();
    }
}