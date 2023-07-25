namespace CombatBeesBenchmarkV2.EcsPrototype;

public interface IWorld
{
    IEnumerable<IEntity<TComponent>> Query<TComponent>() where TComponent : struct;
    void Add<TComponent>(IEntity<TComponent> entity) where TComponent : struct;
    void Remove<TComponent>(IEntity<TComponent> entity) where TComponent : struct;
}