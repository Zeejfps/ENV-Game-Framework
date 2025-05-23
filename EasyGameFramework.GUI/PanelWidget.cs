﻿using OpenGLSandbox;

namespace EasyGameFramework.GUI;

public sealed class PanelWidget : Widget
{
    public PanelStyle Style { get; init; }

    private IRenderedPanel? m_RenderedPanel;
        
    protected override IWidget BuildContent(IBuildContext context)
    {
        return this;
    }

    public override void Layout(IBuildContext context)
    {
        //Console.WriteLine("Build:PanelWidget");
        var renderer = context.PanelRenderer;
        m_RenderedPanel = renderer.Render(ScreenRect, Style);
        base.Layout(context);
    }

    public override void Dispose()
    {
        m_RenderedPanel?.Dispose();
        m_RenderedPanel = null;
    }
}