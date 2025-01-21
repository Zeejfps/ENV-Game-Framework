namespace Bricks.Repos;

public abstract class BaseRepo<T> : IRepo
{
    private readonly HashSet<T> _entitiesToAdd = new();
    private readonly HashSet<T> _entitiesToRemove = new();
    private readonly HashSet<T> _entities = new();
    
    public void Add(T entity)
    {
        _entitiesToAdd.Add(entity);
        _entitiesToRemove.Remove(entity);
    }

    public void Remove(T entity)
    {
        _entitiesToRemove.Add(entity);
        _entitiesToAdd.Remove(entity);
    }

    public void Update()
    {
        foreach (var brick in _entitiesToRemove)
        {
            _entities.Remove(brick);
        }
        _entitiesToRemove.Clear();

        foreach (var brick in _entitiesToAdd)
        {
            _entities.Add(brick);
        }
        _entitiesToAdd.Clear();
    }

    public IEnumerable<T> GetAll()
    {
        return _entities;
    }
}