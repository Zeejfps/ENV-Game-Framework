namespace ZGF.ECSModule;

public readonly struct UpdatedComponent<TComponent>
{
    public required TComponent PrevValue { get; init; }
    public required TComponent NewValue { get; init; }
}

public sealed class ComponentSystem<TEntity, TComponent> : SystemBase 
    where TEntity : notnull
    where TComponent : struct
{
    private readonly Dictionary<TEntity, TComponent> _componentsByEntityLookup = new();
    private readonly Dictionary<TEntity, TComponent> _componentsToRemove = new();
    private readonly Dictionary<TEntity, TComponent> _componentsToAdd = new();
    private readonly Dictionary<TEntity, UpdatedComponent<TComponent>> _componentsToUpdate = new();

    public IEnumerable<KeyValuePair<TEntity, TComponent>> AddedComponents => _componentsToAdd;
    public IEnumerable<KeyValuePair<TEntity, UpdatedComponent<TComponent>>> UpdatedComponents => _componentsToUpdate;
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
        
        foreach (var (entity, updatedComponent) in _componentsToUpdate)
        {
            _componentsByEntityLookup[entity] = updatedComponent.NewValue;
        }
        _componentsToUpdate.Clear();
    }

    public bool TryGetComponent(TEntity entity, out TComponent component)
    {
        return _componentsByEntityLookup.TryGetValue(entity, out component);
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

    public void UpdateComponent(TEntity entity, TComponent newValue)
    {
        if (_componentsByEntityLookup.TryGetValue(entity, out var prevValue))
        {
            _componentsToUpdate[entity] = new UpdatedComponent<TComponent>
            {
                PrevValue = prevValue,
                NewValue = newValue
            };
        }
        else
        {
            _componentsToAdd.Add(entity, newValue);
            _componentsToRemove.Remove(entity);
        }
    }

    public bool WasUpdated(TEntity entity, out UpdatedComponent<TComponent> updatedComponent)
    {
        return _componentsToUpdate.TryGetValue(entity, out updatedComponent);
    }
}