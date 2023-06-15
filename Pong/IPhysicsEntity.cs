namespace Pong;

public interface IPhysicsEntity
{
    PhysicsEntity Save();
    void Load(PhysicsEntity state);
}