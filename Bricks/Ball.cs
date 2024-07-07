using System.Numerics;
using OpenGLSandbox;
using Rect = EasyGameFramework.Api.Physics.Rect;

namespace Bricks;

public sealed class Ball : ISprite
{
    public event Action<IInstancedItem<SpriteInstanceData>>? BecameDirty;
    public void Update(ref SpriteInstanceData instancedData)
    {
        instancedData.Tint = new Color(1f, 1f, 1f, 1f);
        instancedData.ScreenRect = new ScreenRect
        {
            X = 320,
            Y = 240,
            Width = 20,
            Height = 20,
        };
        instancedData.AtlasRect = new ScreenRect
        {
            X = 120f,
            Y = 0f,
            Width = 20f,
            Height = 20f
        };
    }

}