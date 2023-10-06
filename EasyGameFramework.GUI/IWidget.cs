namespace OpenGLSandbox;

public interface IWidget : IDisposable
{
    Rect ScreenRect { get; set; }
    void Update(IBuildContext context);
}