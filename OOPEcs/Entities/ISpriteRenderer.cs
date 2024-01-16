using OpenGLSandbox;

namespace Entities;

public interface ISpriteRenderer
{
    IRenderedSprite Render(Rect screenRect);
}

public interface IRenderedSprite : IDisposable
{
    Rect ScreenRect { get; set; }
}