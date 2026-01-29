namespace ZGF.ECSModule;

public readonly struct UpdatedComponent<TComponent>
{
    public required TComponent PrevValue { get; init; }
    public required TComponent NewValue { get; init; }
}

public sealed class ComponentSystem<TComponent> : SystemBase 
    where TComponent : struct
{
    private readonly Dictionary<Entity, TComponent> _componentsByEntityLookup = new();
    private readonly Dictionary<Entity, TComponent> _componentsToRemove = new();
    private readonly Dictionary<Entity, TComponent> _componentsToAdd = new();
    private readonly Dictionary<Entity, UpdatedComponent<TComponent>> _componentsToUpdate = new();

    public IEnumerable<Entity> Entities => _componentsByEntityLookup.Keys;
    public IEnumerable<KeyValuePair<Entity, TComponent>> AddedComponents => _componentsToAdd;
    public IEnumerable<KeyValuePair<Entity, UpdatedComponent<TComponent>>> UpdatedComponents => _componentsToUpdate;
    public IEnumerable<KeyValuePair<Entity, TComponent>> RemovedComponents => _componentsToRemove;

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

    public bool TryGetComponent(Entity entity, out TComponent component)
    {
        return _componentsByEntityLookup.TryGetValue(entity, out component);
    }
    
    public void AddComponent(Entity entity, TComponent component)
    {
        
        if (!_componentsByEntityLookup.ContainsKey(entity))
        {
            _componentsToAdd.Add(entity, component);
            _componentsToRemove.Remove(entity);
        }
    }

    public void RemoveComponent(Entity entity)
    {
        if (_componentsByEntityLookup.TryGetValue(entity, out var component))
        {
            _componentsToRemove.Add(entity, component);
            _componentsToAdd.Remove(entity);
        }
    }

    public void UpdateComponent(Entity entity, TComponent newValue)
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

    public bool WillUpdate(Entity entity, out UpdatedComponent<TComponent> updatedComponent)
    {
        return _componentsToUpdate.TryGetValue(entity, out updatedComponent);
    }
}