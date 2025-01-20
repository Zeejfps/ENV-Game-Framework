namespace Bricks;

public interface IBrick : IEntity
{
    Rectangle CalculateBoundsRectangle();
    
    bool IsDamaged { get; }
    
    void TakeDamage();
}