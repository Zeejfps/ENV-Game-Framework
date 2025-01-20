namespace Bricks;

public sealed class BricksRepo
{
    private readonly HashSet<Brick> _bricksToAdd = new();
    private readonly HashSet<Brick> _bricksToRemove = new();
    private readonly HashSet<Brick> _bricks = new();
    
    public void Add(Brick brick)
    {
        _bricksToAdd.Add(brick);
        _bricksToRemove.Add(brick);
    }

    public void Remove(Brick brick)
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

    public IEnumerable<Brick> GetAll()
    {
        return _bricks;
    }
}