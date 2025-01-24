using EasyGameFramework.GUI;
using OpenGLSandbox;
using Raylib_CsLo;

namespace Bricks.RaylibBackend;

public sealed class RaylibPanelRenderer : IPanelRenderer
{
    private CommandBuffer _commandBuffer;
    private Shader _panelShader;
    
    public RaylibPanelRenderer(CommandBuffer commandBuffer)
    {
        _commandBuffer = commandBuffer;
        _panelShader = Raylib.LoadShader("Assets/uirect.vert.glsl", "Assets/uirect.frag.glsl");
    }
    
    public IRenderedPanel Render(Rect screenPosition, PanelStyle style)
    {
        var panel = new RaylibPanel(_commandBuffer, screenPosition, style, _panelShader);
        _commandBuffer.Add(panel);
        return panel;
    }
}