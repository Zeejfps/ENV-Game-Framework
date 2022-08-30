namespace EasyGameFramework.API;

public abstract class Game : IGame
{
    public bool IsRunning { get; private set; }
    
    public void Start()
    {
        IsRunning = true;
        OnStart();
    }

    public void Update(float dt)
    {
        OnUpdate(dt);
    }

    public void Quit()
    {
        IsRunning = false;
        OnQuit();
    }

    protected abstract void OnStart();

    protected abstract void OnUpdate(float dt);
    
    protected abstract void OnQuit();
}