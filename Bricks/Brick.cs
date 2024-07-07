using System.Numerics;
using OpenGLSandbox;
using Rect = EasyGameFramework.Api.Physics.Rect;

namespace Bricks;

public sealed class Brick : ISprite
{
    public event Action<IInstancedItem<SpriteInstanceData>>? BecameDirty;

    public ITextureHandle Texture { get; set; }
    public Rect ScreenRect { get; set; }
    public Rect UvRect { get; set; }
    public Vector3 Color { get; set; }

    public void Update(ref SpriteInstanceData instancedData)
    {
        instancedData.ScreenRect = ScreenRect;
        instancedData.Tint = new Color(Color.X, Color.Y, Color.Z, 1f);
    }
}