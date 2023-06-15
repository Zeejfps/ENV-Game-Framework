using EasyGameFramework.Api.Physics;

namespace Pong;

public interface IBoxCollider
{
    Rect AABB { get; }
}