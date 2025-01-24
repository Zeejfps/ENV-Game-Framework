using OpenGLSandbox;

namespace EasyGameFramework.GUI;

public interface IPanelRenderer
{
    IRenderedPanel Render(Rect screenPosition, PanelStyle style);
}