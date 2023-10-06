namespace OpenGLSandbox;

public interface IRenderedPanel : IDisposable
{
    Rect ScreenRect { get; set; }
    PanelStyle Style { get; set; }
}