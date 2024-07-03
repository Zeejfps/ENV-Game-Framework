using OpenGLSandbox;
using Rect = EasyGameFramework.Api.Physics.Rect;

namespace Bricks;

public sealed class Brick : ISprite
{
    public event Action<IInstancedItem<SpriteInstanceData>>? BecameDirty;

    public ITextureHandle Texture { get; set; }
    public Rect ScreenRect { get; set; }
    public Rect UvRect { get; set; }
    
    public void Update(ref SpriteInstanceData instancedData)
    {
        instancedData.ScreenRect = ScreenRect;
    }
}