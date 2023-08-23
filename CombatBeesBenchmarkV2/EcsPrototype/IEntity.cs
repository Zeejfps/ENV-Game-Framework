namespace CombatBeesBenchmarkV2.EcsPrototype;

public interface IEntity<TComponent> : IEntity where TComponent : struct
{
    void Into(out TComponent component);
    void From(ref TComponent component);
}

public interface IEntity
{
    
}