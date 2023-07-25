namespace CombatBeesBenchmarkV2.EcsPrototype;

public interface IWorld
{
    IEnumerable<IEntity<TComponent>> Query<TComponent>() where TComponent : struct;
}