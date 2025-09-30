namespace ZGF.ECSModule;

public abstract class Sim<TEntity>
    where TEntity : notnull
{
    public Clock Clock { get; } = new();
    public WorldSystem<TEntity> World { get; } = new();
    public List<ISystem> Systems { get; } = new();
    
    public Sim()
    {
    }

    public ComponentSystem<TEntity, TComponent> AddComponentSystem<TComponent>() where TComponent : struct
    {
        var system = new ComponentSystem<TEntity, TComponent>();
        Systems.Add(system);
        return system;
    } 

    public void Update(float dt)
    {
        Clock.Tick(dt);
        
        foreach (var system in Systems)
        {
            system.PreUpdate();
        }
        World.PreUpdate();
        
        OnUpdate(dt);
        foreach (var system in Systems)
        {
            system.Update();
        }
        World.Update();

        foreach (var system in Systems)
        {
            system.PostUpdate();
        }
        World.PostUpdate();
    }

    protected virtual void OnUpdate(float dt)
    {
        
    }
}