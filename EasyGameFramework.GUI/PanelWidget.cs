namespace OpenGLSandbox;

public sealed class PanelWidget : Widget
{
    public PanelStyle Style { get; init; }

    private IRenderedPanel? m_RenderedPanel;
        
    protected override IWidget Build(IBuildContext context)
    {
        //Console.WriteLine("Build:PanelWidget");
        var renderer = context.Get<IPanelRenderer>();
        m_RenderedPanel = renderer.Render(ScreenRect, Style);
        return this;
    }

    public override void Dispose()
    {
        m_RenderedPanel?.Dispose();
        m_RenderedPanel = null;
    }
}