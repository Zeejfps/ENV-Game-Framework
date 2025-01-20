namespace Bricks;

public sealed class BricksRepo
{
    private readonly HashSet<IBrick> _bricksToAdd = new();
    private readonly HashSet<IBrick> _bricksToRemove = new();
    private readonly HashSet<IBrick> _bricks = new();
    
    public void Add(IBrick brick)
    {
        _bricksToAdd.Add(brick);
        _bricksToRemove.Remove(brick);
    }

    public void Remove(IBrick brick)
    {
        _bricksToRemove.Add(brick);
        _bricksToAdd.Remove(brick);
    }

    public void Update()
    {
        foreach (var brick in _bricksToRemove)
        {
            _bricks.Remove(brick);
        }
        _bricksToRemove.Clear();

        foreach (var brick in _bricksToAdd)
        {
            _bricks.Add(brick);
        }
        _bricksToAdd.Clear();
    }

    public IEnumerable<IBrick> GetAll()
    {
        return _bricks;
    }
}