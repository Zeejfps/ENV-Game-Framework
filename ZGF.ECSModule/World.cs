namespace ZGF.ECSModule;

public sealed class WorldSystem<TEntity> : SystemBase
{
    private readonly HashSet<TEntity> _entities = new();
    private readonly HashSet<TEntity> _entitiesToSpawn = new();
    private readonly HashSet<TEntity> _entitiesToDespawn = new();
    private readonly HashSet<TEntity> _spawnedEntities = new();
    private readonly HashSet<TEntity> _despawnedEntities = new();

    public IEnumerable<TEntity> Entities => _entities;
    public IEnumerable<TEntity> SpawningEntities => _entitiesToSpawn;
    public IEnumerable<TEntity> DespawningEntities => _despawnedEntities;
    public IEnumerable<TEntity> SpawnedEntities => _spawnedEntities;
    public IEnumerable<TEntity> DespawnedEntities => _despawnedEntities;
    
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
        _spawnedEntities.Clear();
        foreach (var entity in _entitiesToSpawn)
        {
            _entities.Add(entity);
            _spawnedEntities.Add(entity);
        }
        _entitiesToSpawn.Clear();
        
        _despawnedEntities.Clear();
        foreach (var entity in _entitiesToDespawn)
        {
            _entities.Remove(entity);
            _despawnedEntities.Add(entity);
        }
        _entitiesToDespawn.Clear();
    }
}