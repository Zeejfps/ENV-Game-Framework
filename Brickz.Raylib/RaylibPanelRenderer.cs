using EasyGameFramework.GUI;
using OpenGLSandbox;

namespace Bricks.RaylibBackend;

public sealed class RaylibPanelRenderer(CommandBuffer commandBuffer) : IPanelRenderer
{
    public IRenderedPanel Render(Rect screenPosition, PanelStyle style)
    {
        var panel = new RaylibPanel(commandBuffer, screenPosition, style);
        commandBuffer.Add(panel);
        return panel;
    }
}