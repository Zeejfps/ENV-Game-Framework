namespace Bricks.ECS;

public sealed class EntityManager
{
    private uint[] _generations = new uint[1024]; // generation per slot
    private readonly Queue<uint> _freeIndices = new();
    private uint _nextIndex = 0;
    
    public Entity CreateEntity()
    {
        uint index;
        if (_freeIndices.Count > 0)
        {
            index = _freeIndices.Dequeue();
        }
        else
        {
            index = _nextIndex++;
            if (index >= _generations.Length)
                Array.Resize(ref _generations, _generations.Length * 2);
        }
        
        return new Entity(index, _generations[index]);
    }
    
    public void DestroyEntity(Entity entity)
    {
        var index = entity.Index;
        _generations[index]++; // Invalidate all existing references
        _freeIndices.Enqueue(index);
    }
    
    public bool IsAlive(Entity entity)
    {
        return entity.Index < _nextIndex && 
               _generations[entity.Index] == entity.Generation;
    }
    
    public bool IsDestroyed(Entity entity)
    {
        return !IsAlive(entity);
    }
}