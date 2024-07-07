using System.Numerics;
using OpenGLSandbox;
using Rect = EasyGameFramework.Api.Physics.Rect;

namespace Bricks;

public sealed class Brick : ISprite
{
    public event Action<IInstancedItem<SpriteInstanceData>>? BecameDirty;

    public Rect ScreenRect { get; set; }
    public Vector3 Color { get; set; }

    public void Update(ref SpriteInstanceData instancedData)
    {
        instancedData.ScreenRect = ScreenRect;
        instancedData.Tint = new Color(Color.X, Color.Y, Color.Z, 1f);
        instancedData.AtlasRect = new Rect
        {
            BottomLeft = new Vector2(0f, 20f),
            Width = 60f,
            Height = 20f
        };
    }
}