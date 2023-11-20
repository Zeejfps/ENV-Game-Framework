namespace CombatBeesBenchmarkV3.EcsPrototype;

public interface IEntity
{
    
}

public interface IEntity<TArchetype> : IEntity
{
    void WriteTo(ref TArchetype archetype);
    void ReadFrom(ref TArchetype archetype);
}