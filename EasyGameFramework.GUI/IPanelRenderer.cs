namespace OpenGLSandbox;

public interface IPanelRenderer
{
    IRenderedPanel Render(Rect screenPosition, PanelStyle style);
}