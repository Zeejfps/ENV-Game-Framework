using Bricks.PhysicsModule;

namespace Bricks.Archetypes;

public interface IBrick : IEntity
{
    AABB GetAABB();
    
    bool IsDamaged { get; }
    
    void TakeDamage();
}