namespace CombatBeesBenchmarkV2.EcsPrototype;

public interface IEntity<TComponent> : IEntity where TComponent : struct
{
    void Into(ref TComponent component);
    void From(ref TComponent component);
}

public interface IEntity
{
    
}