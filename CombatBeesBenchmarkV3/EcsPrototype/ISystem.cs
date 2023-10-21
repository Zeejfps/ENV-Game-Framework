namespace CombatBeesBenchmarkV3.EcsPrototype;

public interface ISystem
{
    void Tick(float dt);
}

public interface ISystem<in TEntity, TArchetype> : ISystem
{
    void Add(TEntity entity);
    void Remove(TEntity entity);
}