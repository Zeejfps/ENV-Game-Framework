namespace Tetris;

public abstract class Entity : IDisposable
{
    private void Dispose(bool disposing)
    {
        OnDispose(disposing);
    }

    protected abstract void OnDispose(bool disposing);

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~Entity()
    {
        Dispose(false);
    }
}