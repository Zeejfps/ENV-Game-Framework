using System.Numerics;

namespace Pong;

public interface IPhysicsEntity
{
    public Vector2 Position { get; set; }
    public Vector2 Velocity { get; set; }
}