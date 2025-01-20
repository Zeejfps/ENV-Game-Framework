namespace Bricks.Archetypes;

public interface IBrick : IEntity
{
    Rectangle GetAABB();
    
    bool IsDamaged { get; }
    
    void TakeDamage();
}