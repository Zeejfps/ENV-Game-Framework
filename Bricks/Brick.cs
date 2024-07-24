using System.Numerics;
using OpenGLSandbox;

namespace Bricks;

public sealed class Brick : IEntity<Sprite>
{
    public event Action<IEntity<Sprite>>? BecameDirty;

    public ScreenRect ScreenRect { get; set; }
    public Vector3 Color { get; set; }

    public void LoadComponent(ref Sprite sprite)
    {
        sprite.ScreenRect = ScreenRect;
        sprite.Tint = new Color(Color.X, Color.Y, Color.Z, 1f);
        sprite.AtlasRect = new ScreenRect
        {
            X = 0f,
            Y = 20f,
            Width = 60f,
            Height = 20f
        };
    }
}