using OpenGLSandbox;

namespace EasyGameFramework.GUI;

public interface IWidget : IDisposable
{
    Rect ScreenRect { get; set; }
    void Update(IBuildContext context);
    void DoLayout(IBuildContext context);
    Rect Measure(IBuildContext context);
}