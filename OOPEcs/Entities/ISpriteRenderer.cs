using OpenGLSandbox;

namespace Entities;

public interface ISpriteRenderer
{
    IRenderedSprite Render(Rect screenRect);
}

public interface IRenderedSprite
{
    Rect ScreenRect { get; set; }
}