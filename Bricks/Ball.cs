using System.Numerics;
using OpenGLSandbox;
using Rect = EasyGameFramework.Api.Physics.Rect;

namespace Bricks;

public sealed class Ball : ISprite
{
    public event Action<IInstancedItem<SpriteInstanceData>>? BecameDirty;
    public void Update(ref SpriteInstanceData instancedData)
    {
        instancedData.Tint = new Color(0f, 0f, 0f, 1f);
        instancedData.ScreenRect = new Rect
        {
            BottomLeft = new Vector2(30f, 30f),
            Width = 10f,
            Height = 10f,
        };
        instancedData.AtlasRect = new Rect
        {
            BottomLeft = new Vector2(0f, 20f),
            Width = 120f,
            Height = 20f
        };
    }

}