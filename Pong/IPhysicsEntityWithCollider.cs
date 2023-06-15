namespace Pong;

public interface IPhysicsEntityWithCollider
{
    PhysicsEntityWithColliderState Save();
    void Load(PhysicsEntityWithColliderState state);
}