namespace ZGF.ECSModule;

public sealed class WorldSystem<TEntity> : SystemBase
{
    private readonly HashSet<TEntity> _entities = new();
    private readonly HashSet<TEntity> _entitiesToSpawn = new();
    private readonly HashSet<TEntity> _entitiesToDespawn = new();
    
    public IEnumerable<TEntity> Entities => _entities;
    public IEnumerable<TEntity> SpawningEntities => _entitiesToSpawn;
    public IEnumerable<TEntity> DespawningEntities => _entitiesToDespawn;
    
    public void Spawn(TEntity entity)
    {
        _entitiesToSpawn.Add(entity);
        _entitiesToDespawn.Remove(entity);
    }

    public void Despawn(TEntity entity)
    {
        _entitiesToDespawn.Add(entity);
        _entitiesToSpawn.Remove(entity);
    }

    protected override void OnUpdate()
    {
        foreach (var entity in _entitiesToSpawn)
        {
            _entities.Add(entity);
        }
        _entitiesToSpawn.Clear();
        
        foreach (var entity in _entitiesToDespawn)
        {
            _entities.Remove(entity);
        }
        _entitiesToDespawn.Clear();
    }
}