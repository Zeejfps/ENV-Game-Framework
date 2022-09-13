using System.Numerics;
using EasyGameFramework.Api.Physics;

namespace SimplePlatformer;

public interface IRectRenderer
{
    void DrawRect(Rect rect, Vector3 color);
}