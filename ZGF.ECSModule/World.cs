namespace ZGF.ECSModule;

public sealed class WorldSystem : SystemBase
{
    private readonly HashSet<Entity> _entities = new();
    private readonly HashSet<Entity> _entitiesToSpawn = new();
    private readonly HashSet<Entity> _entitiesToDespawn = new();
    private readonly HashSet<Entity> _spawnedEntities = new();
    private readonly HashSet<Entity> _despawnedEntities = new();

    public IEnumerable<Entity> Entities => _entities;
    public IEnumerable<Entity> SpawningEntities => _entitiesToSpawn;
    public IEnumerable<Entity> DespawningEntities => _despawnedEntities;
    public IEnumerable<Entity> SpawnedEntities => _spawnedEntities;
    public IEnumerable<Entity> DespawnedEntities => _despawnedEntities;
    
    public void Spawn(Entity entity)
    {
        _entitiesToSpawn.Add(entity);
        _entitiesToDespawn.Remove(entity);
    }

    public void Despawn(Entity entity)
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