namespace Tetris;

public sealed class MainWorld : IEntity
{
    private Context Context { get; }
    
    public MainWorld(
        Context parentContext
    ){
        Context = new Context(parentContext);
        Context.RegisterTransientEntity<HelloWorldEntity>();
        Context.RegisterTransientEntity<QuitGameInputAction>();
    }

    public void Load()
    {
        Context.Load();
    }

    public void Unload()
    {
        Context.Unload();
    }
}