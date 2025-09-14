namespace ZGF.ECSModule;

public readonly struct UpdatedComponent<TEntity, TComponent>
{
    public required TEntity Entity { get; init; }
    public required TComponent PrevValue { get; init; }
    public required TComponent NewValue { get; init; }
}

public sealed class ComponentSystem<TEntity, TComponent> : System 
    where TEntity : notnull
    where TComponent : struct
{
    private readonly Dictionary<TEntity, TComponent> _componentsByEntityLookup = new();
    private readonly Dictionary<TEntity, TComponent> _componentsToRemove = new();
    private readonly Dictionary<TEntity, TComponent> _componentsToAdd = new();
    private readonly List<UpdatedComponent<TEntity, TComponent>> _componentsToUpdate = new();

    public IEnumerable<KeyValuePair<TEntity, TComponent>> AddedComponents => _componentsToAdd;
    public IEnumerable<UpdatedComponent<TEntity, TComponent>> UpdatedComponents => _componentsToUpdate;
    public IEnumerable<KeyValuePair<TEntity, TComponent>> RemovedComponents => _componentsToRemove;

    protected override void OnUpdate()
    {
        foreach (var (entity, component) in _componentsToRemove)
        {
            _componentsByEntityLookup.Remove(entity);
        }
        _componentsToRemove.Clear();
        
        foreach (var (entity, component) in _componentsToAdd)
        {
            _componentsByEntityLookup[entity] = component;
        }
        _componentsToAdd.Clear();
        
        foreach (var updatedComponent in _componentsToUpdate)
        {
            _componentsByEntityLookup[updatedComponent.Entity] = updatedComponent.NewValue;
        }
        _componentsToUpdate.Clear();
    }
    
    public void AddComponent(TEntity entity, TComponent component)
    {
        if (!_componentsByEntityLookup.ContainsKey(entity))
        {
            _componentsToAdd.Add(entity, component);
            _componentsToRemove.Remove(entity);
        }
    }

    public void RemoveComponent(TEntity entity)
    {
        if (_componentsByEntityLookup.TryGetValue(entity, out var component))
        {
            _componentsToRemove.Add(entity, component);
            _componentsToAdd.Remove(entity);
        }
    }

    public void UpdateComponent(TEntity entity, TComponent component)
    {
        if (_componentsByEntityLookup.TryGetValue(entity, out var prevValue))
        {
            _componentsToUpdate.Add(new UpdatedComponent<TEntity, TComponent>
            {
                Entity = entity,
                PrevValue = prevValue,
                NewValue = component
            });
        }
    }
}