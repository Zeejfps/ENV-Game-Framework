namespace CombatBeesBenchmarkV3.EcsPrototype;

public interface IEntity
{
    
}

public interface IEntity<TArchetype> : IEntity
{
    void Write(ref TArchetype archetype);
    void Read(ref TArchetype archetype);
}