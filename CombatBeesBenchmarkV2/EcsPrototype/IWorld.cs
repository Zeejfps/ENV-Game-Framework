namespace CombatBeesBenchmarkV2.EcsPrototype;

public interface IWorld
{
    int Query<TComponent>(IEntity<TComponent>[] buffer) where TComponent : struct;
    void Add<TComponent>(IEntity<TComponent> entity) where TComponent : struct;
    void Remove<TComponent>(IEntity<TComponent> entity) where TComponent : struct;
}