using System.Numerics;
using EasyGameFramework.Api;

namespace Pong;

public interface ISpriteRenderer
{
    void LoadResources();
    void NewBatch();
    void DrawSprite(Vector2 position, Vector2 scale, Sprite sprite, Vector3 tint);
    void RenderBatch(ICamera camera);
}