using System.Numerics;
using OpenGLSandbox;

namespace Bricks;

public sealed class Brick : ISprite
{
    public event Action<IInstancedItem<SpriteInstanceData>>? BecameDirty;

    public ScreenRect ScreenRect { get; set; }
    public Vector3 Color { get; set; }

    public void UpdateInstanceData(ref SpriteInstanceData instancedData)
    {
        instancedData.ScreenRect = ScreenRect;
        instancedData.Tint = new Color(Color.X, Color.Y, Color.Z, 1f);
        instancedData.AtlasRect = new ScreenRect
        {
            X = 0f,
            Y = 20f,
            Width = 60f,
            Height = 20f
        };
    }
}