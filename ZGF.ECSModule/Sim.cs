namespace ZGF.ECSModule;

public abstract class Sim<TEntity>
    where TEntity : notnull
{
    public Clock Clock { get; } = new();
    public WorldSystem<TEntity> World { get; } = new();
    public List<ISystem> Systems { get; } = new();

    public ComponentSystem<TEntity, TComponent> AddComponentSystem<TComponent>() where TComponent : struct
    {
        var system = new ComponentSystem<TEntity, TComponent>();
        Systems.Add(system);
        return system;
    }

    public void AddSystem(ISystem system)
    {
        Systems.Add(system);   
    }

    public void Update(float dt)
    {
        Clock.Tick(dt);
        
        foreach (var system in Systems)
        {
            system.PreUpdate();
        }
        World.PreUpdate();
        OnPreUpdate();
        
        foreach (var system in Systems)
        {
            system.Update();
        }
        World.Update();
        OnUpdate();

        foreach (var system in Systems)
        {
            system.PostUpdate();
        }
        World.PostUpdate();
        OnPostUpdate();
    }

    protected virtual void OnPreUpdate()
    {
    }

    protected virtual void OnUpdate()
    {
    }
    
    protected virtual void OnPostUpdate(){}
}